# Getting Started

## Installation

Install from NuGet:

```bash
dotnet add package WysiMd.Blazor
```

Or via the Package Manager Console:

```powershell
Install-Package WysiMd.Blazor
```

## Setup

### 1. Register the service

In your `Program.cs`:

```csharp
using WysiMd.Blazor;

builder.Services.AddWysiMdBlazor();
```

### 2. Add static assets

In your `App.razor` (Blazor Web App) or `wwwroot/index.html` (Blazor WASM), add the stylesheet:

```html
<link rel="stylesheet" href="_content/WysiMd.Blazor/css/WysiMd.Blazor.css" />
```

And the script (before `</body>`):

```html
<script src="_content/WysiMd.Blazor/WysiMd.Blazor.js"></script>
```

### 3. Add the global using (optional)

In `_Imports.razor`:

```razor
@using WysiMd.Blazor
@using WysiMd.Blazor.Models
```

## Basic Usage

```razor
@page "/editor"

<MarkdownEditor @bind-Value="content" />

@code {
    private string content = "# Hello, World!\n\nStart editing...";
}
```

## Two-Way Binding

The `Value` parameter uses standard Blazor two-way binding:

```razor
<MarkdownEditor @bind-Value="markdownContent" OnChange="OnContentChanged" />

<div>Characters: @markdownContent.Length</div>

@code {
    private string markdownContent = string.Empty;

    private void OnContentChanged(string newValue)
    {
        // Called on every change — use for autosave, preview updates, etc.
        Console.WriteLine($"Content changed: {newValue.Length} chars");
    }
}
```

## Minimum Requirements

| Requirement | Version |
|---|---|
| .NET | 10.0+ |
| Blazor | Web App or WASM Standalone |
| Browser | Any modern browser (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+) |

## Next Steps

- [Configuration](configuration.md) — all `EditorOptions` parameters
- [Toolbar Customization](toolbar-customization.md) — hide, reorder, or override toolbar items
- [Theming](theming.md) — dark mode and CSS custom properties
- [Mobile](mobile.md) — mobile-first considerations
- [API Reference](api-reference.md) — `MarkdownService` standalone usage
