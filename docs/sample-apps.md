# Sample Applications

One sample app ships with the repository. It is a Blazor WebAssembly project that references the library source directly via `<ProjectReference>`.

> **MudBlazor users:** WysiMd.Blazor works with MudBlazor — no dedicated sample is included in this repo, but see [mudblazor.md](mudblazor.md) for integration examples (cards, dialogs, forms, theme syncing).

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

## Integration Tests

The Playwright test suite targets the vanilla sample at `http://localhost:5100`. Deep routes like `/demo/basic` require SPA fallback routing, which only `dotnet run` provides — `dotnet serve` or plain static hosting will 404.

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

1. Create `Pages/YourDemo.razor` in the sample app with `@page "/demo/your-demo"`.
2. Add a nav link in `Layout/MainLayout.razor`.
3. Add a `<details>` block at the bottom of the page to show the source snippet.
4. If the demo covers a new feature, add a corresponding Playwright test in `WysiMd.Blazor.IntegrationTests`.
