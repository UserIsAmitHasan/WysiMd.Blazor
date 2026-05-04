# Mobile Support

WysiMd.Blazor is designed mobile-first. Every aspect of the editor is tested at 375 px viewport width (iPhone SE baseline).

## Touch Targets

All toolbar buttons are **48 × 48 px** on mobile (≤ 640 px), exceeding the WCAG 2.5.5 minimum of 44 × 44 px.

## Responsive Toolbar

On narrow screens the toolbar scrolls horizontally. The overflow (`···`) panel moves secondary items into a dropdown to keep the primary toolbar uncluttered.

**Recommended overflow items for mobile:**

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        OverflowItems = new List<string>
        {
            "code-block", "hr", "auto-sum", "insert-row", "delete-row",
            "insert-col", "delete-col", "print", "pdf", "download"
        }
    })" />
```

## Dialogs

Link and Table dialogs render as **full-width bottom sheets** on mobile (≤ 640 px), making them easy to dismiss and interact with on small screens.

## Keyboard on Mobile

The virtual keyboard reduces viewport height significantly. The editor sets `min-height` on the textarea, not a fixed height, so it shrinks gracefully when the keyboard appears.

If you set a fixed `Height`, consider making it viewport-relative:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions { Height = "calc(100dvh - 120px)" })" />
```

## Scroll Locking

When a modal dialog is open, body scroll is locked via `overflow: hidden` on `document.body` to prevent double-scroll on iOS Safari.

## Image Uploads on Mobile

Tapping the image upload button opens the native file picker or camera on mobile browsers. Base64 encoding of the image happens in C# via `IJSRuntime`, so no native API restrictions apply.

## Testing Checklist

Before merging UI changes, verify the following on a 375 × 812 px viewport (use browser DevTools device emulation):

- [ ] All toolbar buttons are tappable without zooming
- [ ] Toolbar scrolls horizontally without clipping buttons
- [ ] Link dialog opens as a bottom sheet and is dismissible
- [ ] Table dialog opens as a bottom sheet
- [ ] Typing in the editor doesn't cause layout shifts
- [ ] Status bar wraps gracefully (only word count and mode badge are shown on mobile)
- [ ] Dark mode toggle works
- [ ] Mode toggle (Visual ↔ Raw) works
- [ ] Heading dropdown is tappable and selectable
- [ ] Overflow panel opens and items are tappable

## Known Limitations

- **`document.execCommand` deprecation:** Visual mode uses `execCommand` for formatting in contenteditable. This is deprecated in the Web standard but remains functional in all major mobile browsers as of 2026. A future version will replace it with a Selection API implementation.
- **iOS Safari selection quirks:** Restoring cursor position after toolbar actions may be slightly off in iOS Safari due to its non-standard selection handling. Use the `setSelection` JS interop call if building custom toolbar integrations.
