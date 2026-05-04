# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
