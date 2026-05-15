# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] – 2026-05-15

### Added
- `EditorOptions.DebounceDelay` (default `500ms`) — debounces `ValueChanged` notifications to reduce SignalR round-trips on Blazor Server. Set to `0` to restore per-keystroke behaviour.
- Blazor Server support — the editor is now viable on Blazor Server with responsive typing and no keystroke storms.

### Fixed
- `_historyTimer` was never disposed — minor memory leak now resolved.
- `OnParametersSet` no longer resets editor content mid-typing when the parent re-renders and echoes back the last notified value.

---

## [1.0.3] – 2026-05-11

### Changed
- Sample app now serves documentation as static web assets
- Enhanced navigation layout and styles in the sample app
- Updated README

### Fixed
- Corrected a broken documentation link

### Removed
- MudBlazor (first class support, no worries) sample project

---

## [1.0.2] – 2026-05-05

### Fixed
- Corrected repository URLs in NuGet package metadata

---

## [1.0.1] – 2026-05-04

### Changed
- Updated test framework references to MSTest for unit and integration tests

---

## [1.0.0] – 2026-05-04

### Added
- Visual (WYSIWYG) and Raw (textarea) editing modes
- Toolbar with bold, italic, strikethrough, headings, lists, links, images, table, code, blockquote, horizontal rule, undo/redo
- Overflow panel for secondary toolbar items
- Table insert dialog with row/column editing operations (insert row, delete row, insert column, delete column, auto-sum)
- Link insert dialog
- Dark/light theme with `IsDarkTheme` two-way parameter
- `EditorOptions` for full toolbar and behaviour customisation
- Per-button overrides via `ToolbarItemOverrides` dictionary
- `ReadOnly` mode
- `MarkdownService` public C# API for standalone markdown operations
- Status bar (word count, character count, line count, reading time)
- Smart Enter (list continuation / exit empty list item)
- Tab key inserts 4 spaces in Raw mode
- Keyboard shortcuts (Ctrl+B/I/Z/Y/S/L/K and more)
- Image upload as base64 data URL (max 5 MB)
- Markdown file download (Ctrl+S)
- `OnPrint` and `OnDownloadPdf` callbacks
- Two-way `@bind-Value` and `@bind-FileName` support
- `AddWysiMdBlazor()` DI extension method
