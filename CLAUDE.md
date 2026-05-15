# WysiMd.Blazor — CLAUDE.md

A WYSIWYG Markdown editor Razor Class Library (RCL) for Blazor (.NET 10). Minimal JavaScript — all markdown processing is C#.

## Repository Layout

```
WysiMd.Blazor/
├── WysiMd.Blazor.sln
├── src/
│   └── WysiMd.Blazor/             # Main RCL (ships to NuGet)
│       ├── WysiMd.Blazor.csproj
│       ├── Models/
│       │   ├── EditorModels.cs    # EditorOptions, EditorMode, ToolbarItem, EditorSelection, EditorStats
│       │   └── MarkdownDocument.cs # Content state + undo/redo (max 50 entries, 1 s debounce)
│       ├── Services/
│       │   ├── MarkdownService.cs # Pure-C# markdown API (Markdig rendering + text transforms)
│       │   └── ServiceCollectionExtensions.cs
│       ├── Components/
│       │   ├── MarkdownEditor.razor     # Main component (~462 lines, owns all state)
│       │   ├── EditorToolbar.razor      # Toolbar + overflow panel (SVG icons, 20+ built-in actions)
│       │   ├── LinkDialog.razor         # Modal — insert [text](url)
│       │   └── TableDialog.razor        # Modal — generate GFM table
│       └── wwwroot/
│           ├── WysiMd.Blazor.js         # ~500-line vanilla JS (cursor/selection/DOM table ops)
│           └── css/WysiMd.Blazor.css    # 645 lines, CSS custom properties, responsive
├── tests/
│   ├── WysiMd.Blazor.UnitTests/   # MSTest + bUnit
│   ├── WysiMd.Blazor.IntegrationTests/ # MSTest + Playwright for .NET
│   └── WysiMd.Blazor.JsTests/    # Vitest (vanilla JS functions)
├── samples/
│   └── WysiMd.Blazor.Sample/      # Blazor WASM demo + docs site (integration test target)
└── docs/                          # Markdown documentation (served by the sample app)
    ├── sample-apps.md             # ← csproj details, page inventory, running locally
    └── mudblazor.md               # MudBlazor integration guide (dialogs, forms, theme sync)
```

## Architecture

### Dual Editing Modes
- **Visual (WYSIWYG):** contenteditable div with live HTML preview. Toolbar uses `document.execCommand`. JS `getMarkdownFromHtml()` walks the DOM to reconstruct Markdown on every edit.
- **Raw:** plain `<textarea>`. All formatting transforms are pure C# (`MarkdownService`). Cursor is read/restored via JS `getSelection`/`setSelection`.

### State Flow
```
User types → OnSourceInput / OnWysiwygInput
  → SetContentSilent (no history push)
  → RefreshPreview (re-render HTML)
  → NotifyChange (fire ValueChanged + OnChange callbacks)
  → [1 s debounce] → PushHistory
```

### JS Surface (window.WysiMdBlazor)
All JS is vanilla, no external libraries. Key functions:
- `getSelection(id)` / `setSelection(id, s, e)` / `setValueAndSelection(id, val, s, e)` — cursor management
- `registerShortcuts(id, dotnetRef)` — maps Ctrl+B/I/Z/Y/… to C# `HandleShortcut`
- `registerSelectionListener(id, dotnetRef)` — polls `queryCommandState` in visual mode
- `getMarkdownFromHtml(id)` — DOM-walk to reconstruct Markdown (complex; touch carefully)
- Table DOM ops: `insertRow/deleteRow/insertColumn/deleteColumn/autoSum`
- `downloadFile(filename, dataUrl)` — triggers browser download

### MarkdownService (pure C#)
All methods return `(newMarkdown, newStart, newEnd)` or `(newMarkdown, newCursor)` so the caller can restore cursor position via JS.

Key internal helpers:
- `ToggleInlineWrapper` — handles bold/italic/strikethrough/code with auto-expand-to-word
- `ToggleLinePrefix` — handles lists/blockquotes across multi-line selections
- `GetLineRange` / `GetSelectedLines` — line boundary utilities (no external deps)

### CSS Theming
20+ `--wysimd-*` CSS custom properties. Dark mode via `.wysimd-dark` class on root. Mobile breakpoint at 640 px — buttons 48×48 px, dialogs as bottom sheets.

## Development Commands

```powershell
# Build library
dotnet build src/WysiMd.Blazor

# Run unit tests
dotnet test tests/WysiMd.Blazor.UnitTests

# Run integration tests — sample app must be running first (see below)
dotnet test tests/WysiMd.Blazor.IntegrationTests

# Run JS tests
cd tests/WysiMd.Blazor.JsTests && npm test

# Run Blazor sample (dev server with SPA routing — required before integration tests)
dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100

# Pack NuGet (dry run)
dotnet pack src/WysiMd.Blazor --configuration Release --output ./artifacts
```

## Integration Test Prerequisites

See **`docs/integration-tests.md`** for the full runbook including Playwright version troubleshooting.

**Quick start:**
```powershell
# Terminal 1 — leave running (defaults to http://localhost:5100)
dotnet run --project samples/WysiMd.Blazor.Sample

# Terminal 2
dotnet test tests/WysiMd.Blazor.IntegrationTests
```

**If tests fail with "Executable doesn't exist"** — Playwright package version doesn't match the installed browser. Check `docs/integration-tests.md` for the version table and update the csproj. Current package: `Microsoft.Playwright 1.59.0` (matches `chromium_headless_shell-1217`). Both `Microsoft.Playwright` and `Microsoft.Playwright.MSTest` must be the same version.

## Key Design Rules

1. **No JS dependencies** — vanilla DOM APIs only. Never introduce npm packages into `wwwroot/`.
2. **Mobile first** — all new UI must meet 44 px minimum touch targets. Test at 375 px width.
3. **Cursor fidelity** — every toolbar action must restore cursor/selection after the text transform.
4. **No history pollution** — live typing uses `SetContentSilent`; `PushHistory` is debounced at 1 s.
5. **Pure-C# transforms** — new formatting operations go in `MarkdownService`, not JS.
6. **Parameters are documented** — all public `[Parameter]` properties have XML doc comments.

## Adding a Toolbar Action

1. Add an `id` string constant to `ToolbarItem` in `EditorModels.cs`.
2. Add a `<button>` in `EditorToolbar.razor` (SVG icon, `wysimd-toolbar-btn` class, `title` tooltip).
3. Handle the id in `MarkdownEditor.razor → HandleToolbarAction(string action)`.
4. Add a C# method to `MarkdownService` for the text transform (with tests).
5. Wire keyboard shortcut in `WysiMd.Blazor.js → registerShortcuts()` if needed.
6. Document in `docs/toolbar-customization.md` and `README.md`.

## Adding a CSS Variable

Add to both the `:root` block (light defaults) and the `.wysimd-dark :root` / `.wysimd-dark` override block in `WysiMd.Blazor.css`. Document in `docs/theming.md`.

## Conventions

- **C#:** file-scoped namespaces, nullable enabled, expression-bodied members for simple accessors.
- **Commits:** conventional commits — `feat:`, `fix:`, `docs:`, `chore:`, `test:`.
- **No comments for obvious code** — only add a comment when the *why* is non-obvious.
- **Tests:** every `MarkdownService` method must have MSTest `[TestMethod]` unit tests for toggle-on, toggle-off, and edge cases (empty input, cursor-only selection, multi-line).
- **Playwright tests:** MSTest `[TestMethod]` covering the golden path for each major user flow (type in raw, toggle to visual, use toolbar button, verify output).

## NuGet Publishing

CI runs on push to `main`/`dev` and on PRs. Publishing to NuGet triggers on a `v*.*.*` tag:

```powershell
git tag v1.1.0 && git push origin v1.1.0
```

The publish workflow reads `NUGET_API_KEY` from repository secrets.
