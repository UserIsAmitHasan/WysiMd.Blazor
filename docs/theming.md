# Theming

## Dark Mode

Toggle dark mode via the `IsDarkTheme` parameter (two-way bindable):

```razor
<MarkdownEditor @bind-Value="content" @bind-IsDarkTheme="isDark" />
```

Or set it once via `EditorOptions`:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions { IsDarkTheme = true })" />
```

The editor applies `.wysimd-dark` on its root element and switches all CSS custom properties automatically.

## CSS Custom Properties

Override any of the following on a parent element or `:root` in your own stylesheet. All variables are scoped to `.wysimd-editor`, so they will not leak into surrounding UI.

### Layout & Typography

| Variable | Default (light) | Description |
|---|---|---|
| `--wysimd-font` | system-ui stack | Body font |
| `--wysimd-mono` | monospace stack | Code/raw mode font |
| `--wysimd-font-size` | `1rem` | Base font size |
| `--wysimd-line-height` | `1.6` | Line height in preview |
| `--wysimd-radius` | `6px` | Border radius on buttons and dialogs |

### Colors — Light Mode

| Variable | Default | Description |
|---|---|---|
| `--wysimd-bg` | `#ffffff` | Editor background |
| `--wysimd-bg-toolbar` | `#f8f9fa` | Toolbar background |
| `--wysimd-bg-hover` | `#e9ecef` | Toolbar button hover |
| `--wysimd-bg-active` | `#dee2e6` | Toolbar button active/pressed |
| `--wysimd-text` | `#212529` | Primary text color |
| `--wysimd-text-muted` | `#6c757d` | Status bar and placeholder text |
| `--wysimd-accent` | `#0d6efd` | Links, active button highlight |
| `--wysimd-border` | `#dee2e6` | Border color |
| `--wysimd-code-bg` | `#f1f3f4` | Inline code and code block background |
| `--wysimd-shadow` | `0 1px 3px rgba(0,0,0,.08)` | Dialog drop shadow |

### Colors — Dark Mode (`.wysimd-dark`)

| Variable | Default | Description |
|---|---|---|
| `--wysimd-bg` | `#1e1e2e` | Editor background |
| `--wysimd-bg-toolbar` | `#181825` | Toolbar background |
| `--wysimd-bg-hover` | `#313244` | Toolbar button hover |
| `--wysimd-bg-active` | `#45475a` | Toolbar button active/pressed |
| `--wysimd-text` | `#cdd6f4` | Primary text color |
| `--wysimd-text-muted` | `#6c7086` | Status bar and placeholder |
| `--wysimd-accent` | `#89b4fa` | Links, active button highlight |
| `--wysimd-border` | `#313244` | Border color |
| `--wysimd-code-bg` | `#181825` | Code block background |

## Custom Brand Colors

```css
/* In your app's CSS file */
.wysimd-editor {
  --wysimd-accent: #7c3aed;       /* Purple brand color */
  --wysimd-bg-toolbar: #faf5ff;   /* Tinted toolbar */
}
```

## Custom Background

Pass a CSS color string through `EditorOptions`:

```razor
<MarkdownEditor @bind-Value="content"
    Options="@(new EditorOptions
    {
        Background = "#fefce8",         /* light mode background */
        DarkBackground = "#1c1917"      /* dark mode background */
    })" />
```

## Removing the Border

```css
.wysimd-editor {
  --wysimd-border: transparent;
  box-shadow: none;
}
```

## Matching Your MudBlazor Theme

```razor
@inject MudThemeProvider ThemeProvider

<MarkdownEditor @bind-Value="content"
    Options="@editorOptions"
    @bind-IsDarkTheme="isDark" />

@code {
    private bool isDark;
    private EditorOptions editorOptions => new()
    {
        IsDarkTheme = isDark
    };
}
```

Apply MudBlazor palette values via CSS custom properties in `app.css` to keep both toolkits in sync.
