# Roadmap

This document outlines the planned direction for WysiMd.Blazor. Items are grouped by milestone. Community contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) before picking up a task.

## v1.1 — Editor Polish

- [ ] **Paste as plain text** — strip HTML when pasting into visual mode (`Ctrl+Shift+V`)
- [ ] **Find & Replace** — in-editor search panel (`Ctrl+F` / `Ctrl+H`)
- [ ] **Word wrap toggle** — button to switch between soft-wrap and no-wrap in raw mode
- [ ] **Line numbers** — optional gutter in raw mode (`EditorOptions.ShowLineNumbers`)
- [ ] **Auto-close brackets** — `[`, `(`, `` ` `` auto-close in raw mode
- [ ] **Improved mobile toolbar** — collapsible toolbar groups, swipe-to-reveal secondary actions
- [ ] **Accessibility audit** — full WCAG 2.1 AA pass; ARIA roles on toolbar and dialogs

## v1.2 — Extended Content

- [ ] **Syntax highlighting** — code blocks rendered with a client-side highlighter (no server dependency)
- [ ] **Math / LaTeX** — `$inline$` and `$$block$$` support via MathJax or KaTeX integration
- [ ] **Footnotes** — standard Markdown footnote syntax (`[^1]`)
- [ ] **Emoji picker** — `:emoji_name:` shortcodes with an optional picker panel
- [ ] **Mentions** — `@username` with configurable completion source (callback-based)
- [ ] **Hashtags** — `#tag` detection and styling (callback-based)

## v1.3 — Collaboration & Storage

- [ ] **Autosave** — debounced `localStorage` persistence with restore-on-load option
- [ ] **Diff view** — side-by-side before/after for undo review
- [ ] **Import from HTML** — paste or load HTML and convert to Markdown
- [ ] **Export as HTML** — copy rendered HTML to clipboard
- [ ] **Frontmatter support** — detect and parse YAML/TOML front matter, expose via `FrontMatter` parameter

## v2.0 — Plugin Architecture

- [ ] **Plugin API** — allow third-party Blazor libraries to register toolbar items, keyboard shortcuts, and rendering extensions without forking the library
- [ ] **Custom dialog slots** — `RenderFragment`-based slot for custom dialogs (e.g. media picker)
- [ ] **Toolbar slot API** — inject custom buttons at specific positions via `RenderFragment`
- [ ] **Custom block renderers** — register custom Markdig extensions and paired CSS

## Infrastructure & DX

- [ ] **Blazor Server sample** — alongside the WASM sample, add a Server-side interactive demo
- [ ] **AOT compatibility** — verify and document Blazor WebAssembly AOT publish works
- [ ] **Blazor Hybrid (MAUI) sample** — test and document usage in .NET MAUI Blazor apps
- [ ] **GitHub Codespaces devcontainer** — one-click dev environment
- [ ] **Visual regression tests** — screenshot diff in CI for CSS changes
- [ ] **Benchmark suite** — measure MarkdownService transform throughput and Markdig render time
- [ ] **NuGet ReadMe badge** — auto-generate coverage and test badges from CI

---

## Completed

### v1.0.0 — Initial Release (2026-05-04)

- [x] Visual (WYSIWYG) and Raw editing modes
- [x] 20+ toolbar actions with keyboard shortcuts
- [x] Dark / light theming with CSS custom properties
- [x] GFM table editing (insert/delete rows and columns, auto-sum)
- [x] Image upload as base64 data URL
- [x] Undo/redo with debounced history
- [x] Word/char/line count status bar
- [x] Read-only mode
- [x] Responsive design (mobile-friendly, 640 px breakpoint)
- [x] Configurable toolbar (hide, reorder, override icons/labels)
- [x] Link dialog and Table dialog modals
- [x] NuGet packaging with Source Link and symbol packages
- [x] GitHub Actions CI/CD pipelines

---

> **Want to contribute?** Pick any unchecked item above, open an issue to claim it, then submit a PR. See [CONTRIBUTING.md](CONTRIBUTING.md) for workflow details.
