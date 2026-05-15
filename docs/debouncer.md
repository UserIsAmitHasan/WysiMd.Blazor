# Debouncer — How It Works and How to Use It

## The Problem It Solves

In Blazor Server, every call to `ValueChanged` triggers a SignalR round-trip over WebSocket. If the editor fires `ValueChanged` on every keystroke, typing "hello" causes 5 network round-trips — making the editor feel laggy.

The `Debouncer` delays the action until the user stops doing something for a set amount of time, then fires once.

**Debouncing vs Batching:**
- Batching fires every N ms regardless of activity.
- Debouncing fires only after N ms of *silence*. Continuous typing = zero fires until the user pauses.

---

## How It Works

```
User types → Schedule() called → timer resets
User types → Schedule() called → timer resets
User types → Schedule() called → timer resets
User stops → 500ms passes → action fires once
```

If the user never stops, the action never fires — until blur, where `FlushAsync()` forces it immediately.

### Threading Model

`System.Timers.Timer` fires on a background ThreadPool thread. Blazor component state must only be touched on the Blazor UI thread (synchronization context). The caller is responsible for marshalling via `InvokeAsync` — see the usage example below.

### Dispose Guard

`_disposed` is set to `true` first in `DisposeAsync()`. If the timer fires on a background thread at the exact moment the component is being disposed, the `Elapsed` handler exits immediately without touching component state.

---

## API

```csharp
var debouncer = new Debouncer(
    getDelay: () => Options.DebounceDelay,  // read delay at call time (supports runtime changes)
    action: () => InvokeAsync(NotifyChange), // the thing to debounce
    onException: ex => Console.Error.WriteLine(ex) // optional error handler
);
```

| Method | Description |
|---|---|
| `Schedule()` | Reset the countdown. Call on every event (e.g. keystroke). |
| `Cancel()` | Stop the timer without firing. |
| `FlushAsync()` | Fire immediately, cancel pending. Call on blur or navigation. |
| `IsScheduled` | True if a call is pending. Useful for "unsaved changes" indicators. |
| `DisposeAsync()` | Stop the timer and release resources. Call from the component's `DisposeAsync`. |

---

## Usage in WysiMd.Blazor

### EditorOptions.DebounceDelay

```csharp
// Default: 500ms — recommended for Blazor Server
new EditorOptions { DebounceDelay = 500 }

// Opt out: fires on every keystroke (old behavior — fine for WASM)
new EditorOptions { DebounceDelay = 0 }
```

### Raw Mode (source textarea)

```csharp
private void OnSourceInput(ChangeEventArgs e)
{
    _document.SetContentSilent(newValue); // update instantly (no network)
    RefreshPreview();                      // render instantly (no network)

    _notifyDebouncer ??= new Debouncer(
        getDelay: () => Options.DebounceDelay,
        action: () => InvokeAsync(NotifyChange)); // debounce the network call
    _notifyDebouncer.Schedule();
}
```

Preview updates immediately on every keystroke. Only the parent notification is debounced.

### Visual Mode (contenteditable)

```csharp
private void OnWysiwygInput()
{
    _wysiwygDebouncer ??= new Debouncer(
        getDelay: () => Options.DebounceDelay,
        action: () => InvokeAsync(() => UpdateFromWysiwyg(refreshPreview: false)));
    _wysiwygDebouncer.Schedule();
}
```

The entire DOM→C# sync is debounced because `UpdateFromWysiwyg` does a JS DOM walk — an expensive cross-boundary call.

### Flush on Blur

```csharp
private async Task FlushNotify()
{
    if (_notifyDebouncer is not null) await _notifyDebouncer.FlushAsync();
    if (_wysiwygDebouncer is not null) await _wysiwygDebouncer.FlushAsync();

    if (_mode == EditorMode.Visual)
        await UpdateFromWysiwyg();
    else
        await NotifyChange();
}
```

Called via `@onblur` on both the textarea and the contenteditable div. Ensures the parent always receives the latest value when the user clicks away — even if the debounce timer hasn't fired yet.

### Stale Value Guard

When `ValueChanged` fires, the parent re-renders and passes `Value` back down via `OnParametersSet`. Without a guard, this would reset the editor mid-typing.

```csharp
// In NotifyChange — record what we sent
_lastNotifiedValue = _document.RawMarkdown;

// In OnParametersSet — only reset for truly external values
if (Value != _document.RawMarkdown && Value != _lastNotifiedValue)
{
    _document = new MarkdownDocument(Value);
    _lastNotifiedValue = Value;
    RefreshPreview();
}
```

If `Value == _lastNotifiedValue`, the parent is echoing our own notification — we ignore it safely.

---

## Behavior Matrix

| Scenario | DebounceDelay = 500 | DebounceDelay = 0 |
|---|---|---|
| Keystroke → ValueChanged | After 500ms idle | Every keystroke |
| Toolbar / dialog action | Immediate | Immediate |
| Blur (click away) | Immediate flush | Immediate |
| Parent re-render mid-typing | Editor content preserved | Editor content preserved |
| Component disposed mid-debounce | Action dropped safely | n/a |

---

## Porting to Another Project

`Debouncer.cs` has zero external dependencies — copy the file to any .NET project. The only contract:

1. Pass `getDelay` as a `Func<int>` so delay is read at schedule time (supports runtime changes).
2. Pass `action` as a `Func<Task>` — sync actions wrap as `() => { doWork(); return Task.CompletedTask; }`.
3. If calling from a background thread into a UI framework (Blazor, WinForms, WPF), wrap the action in the appropriate dispatcher (`InvokeAsync`, `Invoke`, `Dispatcher.InvokeAsync`).
4. Call `DisposeAsync()` when the owner is disposed.
