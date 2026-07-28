# WysiMd.Blazor

[![CI](https://github.com/UserIsAmitHasan/WysiMd.Blazor/actions/workflows/ci.yml/badge.svg)](https://github.com/UserIsAmitHasan/WysiMd.Blazor/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/WysiMd.Blazor)](https://www.nuget.org/packages/WysiMd.Blazor)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WysiMd.Blazor)](https://www.nuget.org/packages/WysiMd.Blazor)
[![MIT License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)

**What You See Is Markdown** — a WYSIWYG Markdown editor Razor component for Blazor.
Always produces clean Markdown. Minimal JavaScript. Mobile friendly. Dark & light themes.

---

## Features

- **Visual (WYSIWYG) and Raw modes** — switch instantly, content stays in sync
- **20+ toolbar actions** with keyboard shortcuts (Ctrl+B, Ctrl+I, Ctrl+Z, …)
- **Dark / light theming** — two-way bindable, fully driven by CSS custom properties
- **GFM table editing** — insert/delete rows and columns, auto-sum in Visual mode
- **Image uploads** — embedded as base64 data URLs (max 5 MB)
- **Undo / redo** — debounced history (1 s), up to 50 snapshots
- **Status bar** — word, character, line count and reading time
- **Read-only mode** — disables editing; export and theme actions still work
- **Responsive / mobile-first** — 48 × 48 px touch targets, bottom-sheet dialogs on narrow screens
- **Configurable toolbar** — hide, reorder, or override icons and labels per instance
- **Minimal JS** — only cursor/selection tracking and DOM operations; all Markdown logic is C#

---

## Installation

```bash
dotnet add package WysiMd.Blazor
```

### Setup

**`Program.cs`**
```csharp
using WysiMd.Blazor;

builder.Services.AddWysiMdBlazor();
```

**`App.razor` / `index.html`** — add before `</body>`:
```html
<link rel="stylesheet" href="_content/WysiMd.Blazor/css/WysiMd.Blazor.css" />
<script src="_content/WysiMd.Blazor/WysiMd.Blazor.js"></script>
```

**`_Imports.razor`** (optional convenience):
```razor
@using WysiMd.Blazor
@using WysiMd.Blazor.Models
```

---

## Usage

### Basic two-way binding

```razor
<MarkdownEditor @bind-Value="content" />

@code {
    private string content = "# Hello, World!";
}
```

### With options and callbacks

```razor
<MarkdownEditor @bind-Value="content"
                @bind-FileName="fileName"
                @bind-IsDarkTheme="isDark"
                Options="options"
                OnChange="OnChanged"
                OnPrint="OnPrint"
                OnDownloadPdf="OnDownloadPdf" />

@code {
    private string content = string.Empty;
    private string fileName = "my-document";
    private bool isDark = false;

    private readonly EditorOptions options = new()
    {
        Height            = "500px",
        DefaultMode       = EditorMode.Visual,
        ShowStatusBar     = true,
        SpellCheck        = true,
        MaxLength         = 50_000,
    };

    private void OnChanged(string md) { }
    private void OnPrint(string md) { }
    private void OnDownloadPdf(string md) { }
}
```

### Dark mode

```razor
<MarkdownEditor @bind-Value="content" @bind-IsDarkTheme="isDark" />
```

### Read-only preview

```razor
<MarkdownEditor Value="@source"
    Options="@(new EditorOptions { ReadOnly = true, ShowToolbar = false })" />
```

### Minimal toolbar

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        EnabledToolbarItems = new List<string>
        {
            "bold", "italic", "link", "ul", "ol", "toggle-mode"
        },
        ShowStatusBar = false,
        MinHeight = "160px",
    })" />
```

### Inside an EditForm

```razor
<EditForm Model="model" OnValidSubmit="Submit">
    <DataAnnotationsValidator />
    <MarkdownEditor @bind-Value="model.Body"
        Options="@(new EditorOptions { MinHeight = "300px" })" />
    <ValidationMessage For="() => model.Body" />
    <button type="submit">Save</button>
</EditForm>
```

---

## Editor Modes

| Mode | Description |
|---|---|
| `EditorMode.Visual` | Contenteditable WYSIWYG — live inline editing with toolbar formatting |
| `EditorMode.Raw` | Plain textarea showing raw Markdown source |

Users switch modes via the toolbar toggle button. Default is `EditorMode.Visual`.

---

## Parameters

| Parameter | Type | Description |
|---|---|---|
| `@bind-Value` | `string` | Markdown content (two-way) |
| `@bind-FileName` | `string` | Document filename shown in the overflow panel |
| `@bind-IsDarkTheme` | `bool` | Dark mode toggle (two-way) |
| `Options` | `EditorOptions` | Editor configuration object |
| `OnChange` | `EventCallback<string>` | Fires on every content change |
| `OnPrint` | `EventCallback<string>` | Fires when the Print button is clicked |
| `OnDownloadPdf` | `EventCallback<string>` | Fires when the PDF button is clicked |

### Public methods

Capture the component with `@ref` to call its public API:

```razor
<MarkdownEditor @ref="editor" @bind-Value="content" />
<button @onclick='() => editor.InsertMarkdownAtCaretAsync("**inserted**")'>Insert</button>

@code {
    private MarkdownEditor editor = default!;
}
```

| Method | Description |
|---|---|
| `InsertMarkdownAtCaretAsync(string markdown)` | Inserts markdown at the current caret (or last tracked caret) in either mode. The insert is a single undo step. |

---

## EditorOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultMode` | `EditorMode` | `Visual` | Starting editor mode |
| `ShowToolbar` | `bool` | `true` | Show or hide the toolbar |
| `ShowStatusBar` | `bool` | `true` | Show word / char / line count and reading time |
| `ReadOnly` | `bool` | `false` | Disable editing; export and theme buttons still work |
| `SpellCheck` | `bool` | `true` | Browser spell-check |
| `Placeholder` | `string` | `"Start writing..."` | Placeholder in Raw mode |
| `MinHeight` | `string` | `"400px"` | CSS min-height |
| `Height` | `string?` | `null` | Fixed height (overrides MinHeight) |
| `IsDarkTheme` | `bool` | `false` | Dark mode |
| `Background` | `string?` | `null` | Custom background color (light mode) |
| `DarkBackground` | `string?` | `null` | Custom background color (dark mode) |
| `MaxLength` | `int?` | `null` | Maximum character count |
| `AllowFileNameEditing` | `bool` | `true` | Show filename input in overflow panel |
| `EnabledToolbarItems` | `List<string>` | all items | Ordered list of toolbar item IDs to display |
| `OverflowItems` | `List<string>` | secondary items | Item IDs shown in the `···` overflow dropdown |
| `ToolbarItemOverrides` | `Dictionary<string, ToolbarItemOptions>` | `{}` | Per-item overrides: `Hidden`, `Icon`, `Tooltip`, `CssClass` |
| `DebounceDelay` | `int` | `500` | Ms of inactivity before `ValueChanged` fires. Set to `0` for per-keystroke (Blazor WASM default behaviour). Recommended for Blazor Server. |

---

## Toolbar Items

| ID | Action | Shortcut |
|---|---|---|
| `undo` | Undo | Ctrl+Z |
| `redo` | Redo | Ctrl+Y |
| `bold` | **Bold** | Ctrl+B |
| `italic` | *Italic* | Ctrl+I |
| `strikethrough` | ~~Strikethrough~~ | Ctrl+Shift+X |
| `code` | `Inline code` | Ctrl+\` |
| `heading` | Heading dropdown (H1–H6) | — |
| `ul` | Unordered list | — |
| `ol` | Ordered list | — |
| `task` | Task list | — |
| `blockquote` | Blockquote | Ctrl+Shift+B |
| `hr` | Horizontal rule | — |
| `link` | Insert link dialog | Ctrl+L |
| `image` | Upload image (base64) | Ctrl+K |
| `table` | Insert table dialog | — |
| `code-block` | Fenced code block | — |
| `insert-row` | Insert table row *(Visual only)* | — |
| `delete-row` | Delete table row *(Visual only)* | — |
| `insert-col` | Insert table column *(Visual only)* | — |
| `delete-col` | Delete table column *(Visual only)* | — |
| `auto-sum` | Sum column numbers *(Visual only)* | — |
| `download` | Download `.md` file | Ctrl+S |
| `print` | Print / export | Ctrl+P |
| `pdf` | Download PDF (callback) | — |
| `toggle-mode` | Switch Visual ↔ Raw | — |
| `toggle-theme` | Switch dark ↔ light | — |
| `overflow` | Overflow panel | — |

### Hiding or overriding items

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        ToolbarItemOverrides = new Dictionary<string, ToolbarItemOptions>
        {
            ["pdf"]   = new ToolbarItemOptions { Hidden = true },
            ["bold"]  = new ToolbarItemOptions { Tooltip = "Make it bold" },
        }
    })" />
```

---

## MudBlazor

WysiMd.Blazor works with MudBlazor without any adapter package. Drop `<MarkdownEditor>` inside `MudCard`, `MudDialog`, or `EditForm` exactly as you would any other input component.

```razor
<MudCard>
    <MudCardContent Class="pa-0">
        <MarkdownEditor @bind-Value="content"
            Options="@(new EditorOptions { MinHeight = "300px" })" />
    </MudCardContent>
</MudCard>
```

Sync dark mode by forwarding `MudThemeProvider`'s state to `@bind-IsDarkTheme`, and match your palette via CSS custom properties on `.wysimd-editor`. See [docs/mudblazor.md](docs/mudblazor.md) for full examples (dialogs, forms, theme syncing, border removal).

---

## Theming

### Dark mode

```razor
<MarkdownEditor @bind-Value="content" @bind-IsDarkTheme="isDark" />
```

### CSS custom properties

Override any `--wysimd-*` variable on a parent element:

```css
.my-wrapper {
    --wysimd-accent:     #7c3aed;   /* brand color */
    --wysimd-font:       'Georgia', serif;
    --wysimd-radius:     2px;
    --wysimd-bg-toolbar: #faf5ff;
}
```

Key variables (full list in [docs/theming.md](docs/theming.md)):

| Variable | Light default | Dark default |
|---|---|---|
| `--wysimd-bg` | `#ffffff` | `#1e1e2e` |
| `--wysimd-bg-toolbar` | `#f8f9fa` | `#181825` |
| `--wysimd-text` | `#212529` | `#cdd6f4` |
| `--wysimd-accent` | `#0d6efd` | `#89b4fa` |
| `--wysimd-border` | `#dee2e6` | `#313244` |
| `--wysimd-code-bg` | `#f1f3f4` | `#181825` |

---

## Mobile Support

WysiMd.Blazor is designed mobile-first:

- **48 × 48 px** touch targets on screens ≤ 640 px (exceeds WCAG 2.5.5)
- Toolbar scrolls horizontally — no buttons hidden at small widths
- Link and Table dialogs render as **full-width bottom sheets** on mobile
- Viewport-relative height for full-screen mobile editing:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions { Height = "calc(100dvh - 120px)" })" />
```

See [docs/mobile.md](docs/mobile.md) for the full testing checklist.

---

## MarkdownService C# API

Inject `MarkdownService` for standalone use:

```csharp
// Registration (done by AddWysiMdBlazor)
builder.Services.AddSingleton<MarkdownService>();
```

```csharp
// Rendering
string html = svc.ToHtml("# Hello");

// Inline formatting — returns (newMarkdown, newStart, newEnd)
var (md, s, e) = svc.ToggleBold(markdown, selStart, selEnd);
var (md, s, e) = svc.ToggleItalic(markdown, selStart, selEnd);
var (md, s, e) = svc.ToggleStrikethrough(markdown, selStart, selEnd);
var (md, s, e) = svc.ToggleInlineCode(markdown, selStart, selEnd);

// Block formatting
var (md, cursor) = svc.SetHeading(markdown, cursorPos, level: 2);
var (md, s, e)   = svc.ToggleUnorderedList(markdown, selStart, selEnd);
var (md, s, e)   = svc.ToggleOrderedList(markdown, selStart, selEnd);
var (md, s, e)   = svc.ToggleTaskList(markdown, selStart, selEnd);
var (md, s, e)   = svc.ToggleBlockquote(markdown, selStart, selEnd);
var (md, cursor) = svc.InsertHorizontalRule(markdown, cursorPos);
var (md, s, e)   = svc.InsertLink(markdown, selStart, selEnd, url, text);
var (md, cursor) = svc.InsertCodeBlock(markdown, selStart, selEnd, "csharp");

// Table generation
string table = svc.GenerateTable(rows: 3, cols: 4);
string table = svc.GenerateTable(rows: 3, cols: 2, data: new[] {
    new[] { "Name", "Score" },
    new[] { "Alice", "95" },
});

// Statistics
EditorStats stats = svc.GetStats(markdown);
// stats.WordCount / CharCount / LineCount / ReadingTimeDisplay
```

---

## Repository Structure

```
WysiMd.Blazor/
├── src/
│   └── WysiMd.Blazor/             # Library — ships to NuGet
│       ├── WysiMd.Blazor.csproj
│       ├── Models/                # EditorOptions, MarkdownDocument, EditorStats
│       ├── Services/              # MarkdownService, DI registration
│       ├── Components/            # MarkdownEditor, EditorToolbar, LinkDialog, TableDialog
│       └── wwwroot/               # WysiMd.Blazor.js + WysiMd.Blazor.css
│
├── tests/
│   ├── WysiMd.Blazor.UnitTests/   # MSTest + bUnit
│   ├── WysiMd.Blazor.IntegrationTests/ # MSTest + Playwright for .NET
│   └── WysiMd.Blazor.JsTests/    # Vitest (jsdom) — vanilla JS functions
│
├── samples/
│   ├── WysiMd.Blazor.Sample/      # Blazor WASM standalone — 7 demo pages
│
└── docs/                          # Markdown documentation
    ├── getting-started.md
    ├── configuration.md
    ├── toolbar-customization.md
    ├── theming.md
    ├── mudblazor.md
    ├── mobile.md
    └── api-reference.md
```

---

## Running the Samples

```bash
dotnet run --project samples/WysiMd.Blazor.Sample
```

---

## Running Tests

```bash
# Unit tests
dotnet test tests/WysiMd.Blazor.UnitTests

# JS tests (requires Node 18+)
cd tests/WysiMd.Blazor.JsTests
npm ci && npm test

# Integration tests (start the sample first)
dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100 &
dotnet test tests/WysiMd.Blazor.IntegrationTests
```

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Markdig` | 1.1.2 | Markdown → HTML (CommonMark + GFM) |
| `Microsoft.AspNetCore.Components.Web` | 10.0.5 | Blazor |

No JavaScript dependencies — all JS is vanilla.

---

## Documentation

| Topic | File |
|---|---|
| Getting Started | [docs/getting-started.md](docs/getting-started.md) |
| Configuration reference | [docs/configuration.md](docs/configuration.md) |
| Toolbar customization | [docs/toolbar-customization.md](docs/toolbar-customization.md) |
| Theming & CSS variables | [docs/theming.md](docs/theming.md) |
| MudBlazor integration | [docs/mudblazor.md](docs/mudblazor.md) |
| Mobile support | [docs/mobile.md](docs/mobile.md) |
| C# API reference | [docs/api-reference.md](docs/api-reference.md) |
| Roadmap | [ROADMAP.md](ROADMAP.md) |
| Changelog | [CHANGELOG.md](CHANGELOG.md) |
| Contributing | [CONTRIBUTING.md](CONTRIBUTING.md) |

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a PR.

- Check [ROADMAP.md](ROADMAP.md) for planned features — open an issue to claim one
- Branch off `dev`, use [conventional commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `test:`)
- All new `MarkdownService` methods need unit tests
- UI changes must be tested at 375 px viewport width
- Update `CHANGELOG.md` in your PR

---

## License

[MIT](LICENSE) © 2025–2026 Autorior
