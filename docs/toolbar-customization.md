# Toolbar Customization

## Built-in Toolbar Item IDs

| ID | Action | Default Shortcut |
|---|---|---|
| `undo` | Undo | Ctrl+Z |
| `redo` | Redo | Ctrl+Y |
| `bold` | Bold | Ctrl+B |
| `italic` | Italic | Ctrl+I |
| `strikethrough` | Strikethrough | Ctrl+Shift+X |
| `code` | Inline code | Ctrl+\` |
| `heading` | Heading dropdown | — |
| `ul` | Unordered list | — |
| `ol` | Ordered list | — |
| `task` | Task list | — |
| `blockquote` | Blockquote | Ctrl+Shift+B |
| `hr` | Horizontal rule | — |
| `link` | Insert link | Ctrl+L |
| `image` | Upload image | Ctrl+K |
| `table` | Insert table | — |
| `code-block` | Fenced code block | — |
| `insert-row` | Insert table row *(visual only)* | — |
| `delete-row` | Delete table row *(visual only)* | — |
| `insert-col` | Insert table column *(visual only)* | — |
| `delete-col` | Delete table column *(visual only)* | — |
| `auto-sum` | Sum column *(visual only)* | — |
| `download` | Download `.md` file | Ctrl+S |
| `print` | Print / export | Ctrl+P |
| `pdf` | Download PDF (callback) | — |
| `toggle-mode` | Switch Visual ↔ Raw | — |
| `toggle-theme` | Switch dark ↔ light | — |
| `overflow` | Overflow panel button | — |

## Reorder and Filter Items

Use `EnabledToolbarItems` to define exactly which items appear and in what order:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        EnabledToolbarItems = new List<string>
        {
            "bold", "italic", "heading", "ul", "ol", "link", "table",
            "toggle-mode", "toggle-theme"
        }
    })" />
```

## Move Items to the Overflow Panel

Items listed in `OverflowItems` are hidden from the main toolbar and shown in the `···` overflow dropdown:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        OverflowItems = new List<string> { "pdf", "print", "download", "hr", "code-block" }
    })" />
```

## Override an Item's Icon, Label, or Visibility

Use `ToolbarItemOverrides` to customise individual items without replacing the whole toolbar:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        ToolbarItemOverrides = new Dictionary<string, ToolbarItemOptions>
        {
            // Hide an item entirely
            ["pdf"] = new ToolbarItemOptions { Hidden = true },

            // Replace SVG icon (inline SVG string)
            ["bold"] = new ToolbarItemOptions
            {
                Icon = "<svg viewBox='0 0 24 24'>...</svg>",
                Tooltip = "Make Bold"
            },

            // Change the tooltip label
            ["toggle-theme"] = new ToolbarItemOptions { Tooltip = "Dark Mode" },
        }
    })" />
```

## ToolbarItemOptions Properties

| Property | Type | Description |
|---|---|---|
| `Hidden` | `bool` | Remove the item from the toolbar entirely |
| `Icon` | `string?` | Inline SVG to replace the default icon |
| `Tooltip` | `string?` | Override the tooltip / `title` attribute |
| `CssClass` | `string?` | Additional CSS classes on the button |

## Minimal Toolbar Example

```razor
<MarkdownEditor @bind-Value="comment"
    Options="@(new EditorOptions
    {
        ShowStatusBar = false,
        EnabledToolbarItems = new List<string> { "bold", "italic", "link", "ul" },
        MinHeight = "120px"
    })" />
```
