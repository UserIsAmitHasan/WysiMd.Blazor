# MudBlazor Integration

WysiMd.Blazor works with [MudBlazor](https://mudblazor.com/) out of the box — no adapter package required. The editor is a standard Razor component and slots into any MudBlazor layout.

## Installation

Follow the standard setup in [getting-started.md](getting-started.md). No extra steps are needed for MudBlazor — just ensure both libraries are registered.

```csharp
// Program.cs
builder.Services.AddMudServices();
builder.Services.AddWysiMdBlazor();
```

---

## Basic Usage

Drop `<MarkdownEditor>` anywhere you would place a MudBlazor input:

```razor
@using WysiMd.Blazor
@using WysiMd.Blazor.Models

<MudPaper Class="pa-4">
    <MarkdownEditor @bind-Value="content" />
</MudPaper>

@code {
    private string content = string.Empty;
}
```

---

## Inside MudCard

```razor
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">Post Body</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent Class="pa-0">
        <MarkdownEditor @bind-Value="content"
            Options="@(new EditorOptions { ShowStatusBar = true, MinHeight = "300px" })" />
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Save">Save</MudButton>
    </MudCardActions>
</MudCard>
```

---

## Inside MudDialog

```razor
@inject IDialogService DialogService

<MudButton OnClick="OpenEditor">Edit Content</MudButton>

@code {
    private async Task OpenEditor()
    {
        var parameters = new DialogParameters
        {
            ["InitialContent"] = content
        };
        var dialog = await DialogService.ShowAsync<EditorDialog>("Edit", parameters);
        var result = await dialog.Result;
        if (!result.Canceled)
            content = (string)result.Data!;
    }
}
```

**`EditorDialog.razor`:**

```razor
@using WysiMd.Blazor
@using WysiMd.Blazor.Models

<MudDialog>
    <DialogContent>
        <MarkdownEditor @bind-Value="localContent"
            Options="@(new EditorOptions { MinHeight = "400px" })" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit">Save</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string InitialContent { get; set; } = string.Empty;

    private string localContent = string.Empty;

    protected override void OnInitialized() => localContent = InitialContent;

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(localContent));
}
```

---

## Inside EditForm with Validation

WysiMd.Blazor binds to a plain `string` property, so it integrates with `EditForm` and `DataAnnotationsValidator` the same way as any text input.

```razor
@using System.ComponentModel.DataAnnotations

<EditForm Model="model" OnValidSubmit="Submit">
    <DataAnnotationsValidator />
    <MudGrid>
        <MudItem xs="12">
            <MudTextField @bind-Value="model.Title" Label="Title" />
            <ValidationMessage For="() => model.Title" />
        </MudItem>
        <MudItem xs="12">
            <MudText Typo="Typo.subtitle2" Class="mb-1">Body</MudText>
            <MarkdownEditor @bind-Value="model.Body"
                Options="@(new EditorOptions { MinHeight = "260px" })" />
            <ValidationMessage For="() => model.Body" />
        </MudItem>
    </MudGrid>
    <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled" Color="Color.Primary" Class="mt-4">
        Submit
    </MudButton>
</EditForm>

@code {
    private PostModel model = new();

    private class PostModel
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required, MinLength(10)] public string Body { get; set; } = string.Empty;
    }

    private void Submit() { /* save model */ }
}
```

---

## Syncing Dark Mode with MudBlazor

MudBlazor manages dark mode through `MudThemeProvider`. Forward its state to the editor via `@bind-IsDarkTheme`:

```razor
<MudThemeProvider @ref="themeProvider" @bind-IsDarkMode="isDark" />
<MarkdownEditor @bind-Value="content" @bind-IsDarkTheme="isDark" />

@code {
    private MudThemeProvider themeProvider = default!;
    private bool isDark;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isDark = await themeProvider.GetSystemPreference();
            StateHasChanged();
        }
    }
}
```

---

## Matching MudBlazor Palette Colors

Map your MudBlazor theme's palette values to WysiMd.Blazor's CSS custom properties so the editor blends into your design:

```css
/* app.css — applied globally */
.wysimd-editor {
    --wysimd-accent:     #594ae2;   /* MudBlazor default primary */
    --wysimd-radius:     4px;
    --wysimd-font:       Roboto, sans-serif;
}
```

For a custom `MudTheme`, read palette values in C# and pass them via a style attribute:

```razor
@inject IThemeProvider MudTheme

<div style="@editorVars">
    <MarkdownEditor @bind-Value="content" />
</div>

@code {
    private string editorVars =>
        $"--wysimd-accent: {MudTheme.Palette.Primary}; " +
        $"--wysimd-radius: {MudTheme.LayoutProperties.DefaultBorderRadius};";
}
```

Full list of CSS variables is in [theming.md](theming.md).

---

## Removing the Editor Border in MudBlazor Layouts

MudBlazor cards and papers already provide visual containment. Remove the editor's own border to avoid double-borders:

```css
.wysimd-editor {
    --wysimd-border: transparent;
    box-shadow: none;
}
```

Or scope it to a specific instance via a wrapper class:

```razor
<div class="no-border-editor">
    <MarkdownEditor @bind-Value="content" />
</div>
```

```css
.no-border-editor .wysimd-editor {
    --wysimd-border: transparent;
    box-shadow: none;
}
```
