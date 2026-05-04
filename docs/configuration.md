# Configuration

All configuration is passed via the `Options` parameter on `<MarkdownEditor>`.

## EditorOptions Reference

```razor
<MarkdownEditor @bind-Value="content" Options="options" />

@code {
    private EditorOptions options = new()
    {
        DefaultMode = EditorMode.Visual,
        ShowToolbar = true,
        ShowStatusBar = true,
        ReadOnly = false,
        SpellCheck = true,
        Placeholder = "Start writing...",
        MinHeight = "400px",
        Height = null,
        IsDarkTheme = false,
        MaxLength = null,
        AllowFileNameEditing = true,
    };
}
```

## All Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultMode` | `EditorMode` | `Visual` | Starting editor mode (`Visual` or `Raw`) |
| `ShowToolbar` | `bool` | `true` | Show or hide the toolbar |
| `ShowStatusBar` | `bool` | `true` | Show word/char/line count and reading time |
| `ReadOnly` | `bool` | `false` | Disable editing; export/theme buttons still work |
| `SpellCheck` | `bool` | `true` | Browser spell-check on the editor |
| `Placeholder` | `string` | `"Start writing..."` | Placeholder text in Raw mode |
| `MinHeight` | `string` | `"400px"` | CSS min-height of the editor |
| `Height` | `string?` | `null` | Fixed height (e.g. `"600px"`); overrides `MinHeight` |
| `IsDarkTheme` | `bool` | `false` | Enable dark mode |
| `Background` | `string?` | `null` | Custom background color for light mode |
| `DarkBackground` | `string?` | `null` | Custom background color for dark mode |
| `MaxLength` | `int?` | `null` | Maximum character count (enforced in Raw mode) |
| `AllowFileNameEditing` | `bool` | `true` | Show filename input in the overflow panel |
| `EnabledToolbarItems` | `List<string>` | all items | Ordered list of toolbar item IDs to display |
| `OverflowItems` | `List<string>` | secondary items | Item IDs to show in the overflow dropdown |
| `ToolbarItemOverrides` | `Dictionary<string, ToolbarItemOptions>` | `{}` | Per-item overrides (hide, re-icon, re-label) |

## Component Parameters

In addition to `Options`, `MarkdownEditor` exposes these top-level parameters:

| Parameter | Type | Description |
|---|---|---|
| `@bind-Value` | `string` | The markdown content (two-way) |
| `@bind-FileName` | `string` | Document filename shown in overflow panel |
| `@bind-IsDarkTheme` | `bool` | Dark mode toggle (two-way; can also be set in `Options`) |
| `OnChange` | `EventCallback<string>` | Fires on every content change |
| `OnPrint` | `EventCallback<string>` | Fires when the Print button is clicked |
| `OnDownloadPdf` | `EventCallback<string>` | Fires when the PDF button is clicked (you provide the logic) |

## Examples

### Fixed-height editor

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions { Height = "500px" })" />
```

### Read-only preview

```razor
<MarkdownEditor Value="@markdownSource"
    Options="@(new EditorOptions { ReadOnly = true, ShowToolbar = false })" />
```

### Dark mode with two-way binding

```razor
<MarkdownEditor @bind-Value="content" @bind-IsDarkTheme="isDark" />

<button @onclick="() => isDark = !isDark">Toggle Theme</button>

@code {
    private string content = string.Empty;
    private bool isDark = false;
}
```

### Character limit

```razor
<MarkdownEditor @bind-Value="bio"
    Options="@(new EditorOptions { MaxLength = 500, ShowStatusBar = true })" />
```
