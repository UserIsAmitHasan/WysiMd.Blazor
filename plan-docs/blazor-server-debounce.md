# Plan: Blazor Server Friendly — Debounced ValueChanged (Revised)

## Context
Every keystroke fires `ValueChanged` → SignalR round-trip per character on Server → unusable. The fix keeps live state in the browser, debounces both **C# → parent notifications** (raw mode) and **DOM → C# sync** (visual mode) at ~500ms idle, flushes on blur, and protects against in-progress edits being clobbered by stale parent `Value`.

## Files to Modify
- `src/WysiMd.Blazor/Components/MarkdownEditor.razor`
- `src/WysiMd.Blazor/Models/EditorModels.cs`

(`WysiMd.Blazor.js` — no changes.)

---

## Changes

### 1. `EditorModels.cs` — Add `DebounceDelay` to `EditorOptions`
```csharp
/// <summary>Delay in ms before ValueChanged fires after typing stops. 0 = immediate. Default 500.</summary>
public int DebounceDelay { get; set; } = 500;
```

---

### 2. `MarkdownEditor.razor` — New fields
```csharp
private System.Timers.Timer? _debounceTimer;    // debounces NotifyChange (raw mode)
private System.Timers.Timer? _wysiwygTimer;     // debounces UpdateFromWysiwyg (visual mode)
private bool _suppressNextInput;                // guards against double-fire from SetValueAndSelection
private string _lastNotifiedValue = string.Empty; // prevents OnParametersSet clobber
private bool _disposed;
```

### 3. `OnParametersSet` — Guard against stale Value during debounce window
```csharp
protected override void OnParametersSet()
{
    _isDark = IsDarkTheme || Options.IsDarkTheme;

    // Only reset when the parent pushed a TRULY external value.
    // If Value matches what we last notified, the parent is just echoing our own change.
    if (Value != _document.RawMarkdown && Value != _lastNotifiedValue)
    {
        _document = new MarkdownDocument(Value);
        _lastNotifiedValue = Value;
        RefreshPreview();
    }
}
```

### 4. `OnSourceInput` — debounce NotifyChange, suppress reflected input
```csharp
private void OnSourceInput(ChangeEventArgs e)
{
    if (Options.ReadOnly) return;
    if (_suppressNextInput) { _suppressNextInput = false; return; }

    string newValue = e.Value?.ToString() ?? string.Empty;
    _historyTimer ??= CreateHistoryTimer();
    _historyTimer.Stop();
    _historyTimer.Start();

    _document.SetContentSilent(newValue);
    RefreshPreview();
    ScheduleNotify();
}
```
(Note: handler becomes synchronous — no await needed.)

### 5. `OnWysiwygInput` — debounce the *entire* DOM→C# sync, not just NotifyChange
```csharp
private void OnWysiwygInput()
{
    if (Options.ReadOnly) return;
    ScheduleWysiwygSync();
}

private void ScheduleWysiwygSync()
{
    if (Options.DebounceDelay <= 0)
    {
        _ = UpdateFromWysiwyg(refreshPreview: false);
        return;
    }
    _wysiwygTimer ??= new System.Timers.Timer { AutoReset = false };
    _wysiwygTimer.Interval = Options.DebounceDelay;
    _wysiwygTimer.Stop();
    // Re-bind Elapsed once
    if (_wysiwygTimer.GetInvocationListSafe() == 0)
        _wysiwygTimer.Elapsed += (s, e) =>
        {
            if (_disposed) return;
            _ = InvokeAsync(() => UpdateFromWysiwyg(refreshPreview: false));
        };
    _wysiwygTimer.Start();
}
```
(GetInvocationListSafe is shorthand — actual impl: track a `_wysiwygTimerBound` bool to bind once.)

### 6. `UpdateFromWysiwyg` — unchanged behavior, fires NotifyChange immediately
Keeps current code. All ~8 existing call sites (toolbar, mode switch, dialogs, FlushNotify) continue to work without modification — they always behaved immediately and still do.

### 7. `ScheduleNotify` — debounce for raw mode
```csharp
private void ScheduleNotify()
{
    if (Options.DebounceDelay <= 0)
    {
        _ = InvokeAsync(NotifyChange);
        return;
    }
    if (_debounceTimer is null)
    {
        _debounceTimer = new System.Timers.Timer { AutoReset = false };
        _debounceTimer.Elapsed += (s, e) =>
        {
            if (_disposed) return;
            _ = InvokeAsync(NotifyChange);
        };
    }
    _debounceTimer.Interval = Options.DebounceDelay;   // re-read each schedule
    _debounceTimer.Stop();
    _debounceTimer.Start();
}
```

### 8. `NotifyChange` — track last-notified value
```csharp
private async Task NotifyChange()
{
    _lastNotifiedValue = _document.RawMarkdown;
    await ValueChanged.InvokeAsync(_document.RawMarkdown);
    await OnChange.InvokeAsync(_document.RawMarkdown);
}
```

### 9. Blur handler — flush both debounces, sync DOM first in visual mode
Razor template — add to both `<textarea>` and contenteditable `<div>`:
```razor
@onblur="FlushNotify"
```

```csharp
private async Task FlushNotify()
{
    _debounceTimer?.Stop();
    _wysiwygTimer?.Stop();

    if (_mode == EditorMode.Visual)
        await UpdateFromWysiwyg();   // pulls latest DOM into _document AND fires NotifyChange
    else
        await NotifyChange();
}
```

### 10. `SetValueAndSelection` — suppress the reflected input event
```csharp
private async Task SetValueAndSelection(string value, int start, int end)
{
    _suppressNextInput = true;
    await SafeJs("WysiMdBlazor.setValueAndSelection", _sourceId, value, start, end);
}
```

### 11. `DisposeAsync` — stop & dispose new timers
```csharp
public async ValueTask DisposeAsync()
{
    _disposed = true;
    _debounceTimer?.Stop(); _debounceTimer?.Dispose();
    _wysiwygTimer?.Stop();  _wysiwygTimer?.Dispose();
    _historyTimer?.Stop();  _historyTimer?.Dispose();
    _dotnetRef?.Dispose();
    if (_jsModule is not null) { try { await _jsModule.DisposeAsync(); } catch { } }
}
```
(Note: existing code didn't dispose `_historyTimer` — minor leak fix bundled.)

---

## Behavior Matrix

| Scenario                          | WASM (Before) | WASM (After) | Server (Before) | Server (After) |
|-----------------------------------|---------------|--------------|-----------------|-----------------|
| Raw keystroke → ValueChanged      | Every key     | 500ms idle   | Every key (laggy) | 500ms idle    |
| Visual keystroke → DOM sync       | Every key     | 500ms idle   | Every key (laggy) | 500ms idle    |
| Toolbar / dialog / shortcut       | Immediate     | Immediate    | Immediate       | Immediate       |
| Blur (focus away)                 | (no-op)       | Flush both   | (no-op)         | Flush both      |
| Parent re-render mid-typing       | Safe (no gap) | Safe (last-notified guard) | Safe | Safe |
| `DebounceDelay = 0`               | n/a           | Old behavior | n/a             | Old behavior    |

---

## Verification
1. `dotnet build src/WysiMd.Blazor` clean.
2. `dotnet test tests/WysiMd.Blazor.UnitTests` — existing tests should pass (debounce is transparent at the API level).
3. Run sample app (WASM) and verify:
   - Typing in raw and visual modes: feel unchanged, preview updates instantly.
   - Toolbar bold / italic / table / link / image: fire and reflect immediately.
   - Click away from editor: parent receives `ValueChanged` immediately.
   - With a sibling component that re-renders every 200ms via a timer: type continuously, verify the editor's content is NOT reset mid-typing.
   - Set `DebounceDelay = 0`: behavior matches pre-change.
4. **New: Blazor Server smoke test.** Create a minimal Server host project (or temporarily change sample app's render mode) and verify typing feels responsive.

---

## Outstanding Questions Worth Noting
This plan addresses the *keystroke-storm* problem, which is the dominant issue. A few smaller Server-mode concerns remain unaddressed by this plan but should be noted:
- Image uploads stream 5MB files through SignalR — slow but functional.
- The selection-listener (`selectionchange`) calls `UpdateActiveFormats` back to C# frequently while the cursor moves; on Server this could be chatty. Could be debounced too (small follow-up).

These can be follow-ups; they don't block Server support being *viable*.
