# Sample Applications

Two sample apps ship with the repository. Both are Blazor WebAssembly projects that reference the library source directly via `<ProjectReference>`.

## WysiMd.Blazor.Sample (Vanilla)

**Path:** `samples/WysiMd.Blazor.Sample/`

The primary demo and documentation site. No UI framework dependencies — plain HTML/CSS only. This app is also the target of the Playwright integration test suite.

### Pages

| Route | File | Purpose |
|---|---|---|
| `/` | `Pages/Home.razor` | Landing page + quick-install |
| `/demo/basic` | `Pages/BasicDemo.razor` | Minimal `@bind-Value` usage |
| `/demo/options` | `Pages/OptionsDemo.razor` | Interactive `EditorOptions` playground |
| `/demo/dark-theme` | `Pages/DarkThemeDemo.razor` | `@bind-IsDarkTheme` toggle |
| `/demo/readonly` | `Pages/ReadOnlyDemo.razor` | Read-only / preview mode |
| `/demo/toolbar` | `Pages/ToolbarDemo.razor` | Toolbar customization |
| `/demo/form` | `Pages/FormDemo.razor` | `EditForm` + `DataAnnotations` |
| `/demo/mobile` | `Pages/MobileDemo.razor` | Mobile-optimised toolbar |
| `/docs/{DocName}` | `Pages/DocPage.razor` | Renders `docs/*.md` via HTTP + `MarkdownService` |

### csproj specifics

```xml
<!-- Links docs/*.md into wwwroot/docs/ so DocPage can fetch them at runtime -->
<ItemGroup>
  <Content Include="..\..\docs\*.md"
           Link="wwwroot/docs/%(Filename)%(Extension)"
           CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

The `docs/` folder lives outside the WASM project. Since WASM cannot read the filesystem at runtime, the files are linked into `wwwroot/docs/` at build time and served as static HTTP assets. `DocPage.razor` fetches them via `HttpClient` and renders them with `MarkdownService.ToHtml()`.

### Services registered in Program.cs

```csharp
// Required for DocPage — fetches docs/*.md from wwwroot
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Registers MarkdownService + editor internals
builder.Services.AddWysiMdBlazor();
```

### Running locally

```powershell
dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
```

This app **must be running** before executing the integration test suite (see [Integration Tests](#integration-tests) below).

---

## WysiMd.Blazor.MudBlazor.Sample

**Path:** `samples/WysiMd.Blazor.MudBlazor.Sample/`

Demonstrates integration with [MudBlazor](https://mudblazor.com/) v9. Includes the same documentation sidebar (fetching the same `docs/*.md`) and additional demos showing the editor inside `MudCard`, `MudDialog`, and `MudForm`.

### Pages

| Route | Purpose |
|---|---|
| `/demo/basic` | Basic usage with MudBlazor layout |
| `/demo/form` | `EditForm` + `MudTextField` + validation |
| `/demo/dialog` | Editor inside `MudDialog` |
| `/demo/card` | Editor inside `MudCard` with Save/Preview |
| `/docs/{DocName}` | Same doc-rendering pattern as vanilla sample |

---

## Integration Tests

The Playwright test suite targets the **vanilla sample** at `http://localhost:5100`. Deep routes like `/demo/basic` require SPA fallback routing, which only `dotnet run` provides — `dotnet serve` or plain static hosting will 404.

```powershell
# Terminal 1 — leave running
dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100

# Terminal 2
dotnet test tests/WysiMd.Blazor.IntegrationTests
```

If Playwright browsers are missing:

```powershell
& "tests\WysiMd.Blazor.IntegrationTests\bin\Debug\net10.0\playwright.ps1" install chromium
```

---

## Adding a New Demo Page

1. Create `Pages/YourDemo.razor` in both sample apps with `@page "/demo/your-demo"`.
2. Add a nav link in `Layout/MainLayout.razor` for both apps.
3. Add a "View Source" `<details>` block (vanilla) or `MudExpansionPanel` (MudBlazor) at the bottom of the page.
4. If the demo covers a new feature, add a corresponding Playwright test in `WysiMd.Blazor.IntegrationTests`.
