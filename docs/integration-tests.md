# Running Integration Tests

## Prerequisites

### 1. Playwright browser version must match the package

The single most common failure is a Playwright version mismatch — the package expects a specific Chromium revision that isn't installed.

**Installed browsers live at:**
```
C:\Users\<you>\AppData\Local\ms-playwright\
```

**Check what's installed:**
```powershell
ls "$env:LOCALAPPDATA\ms-playwright"
# e.g. chromium_headless_shell-1217
```

**The package version that ships each Chromium revision:**

| ms-playwright folder | Microsoft.Playwright NuGet |
|---|---|
| chromium_headless_shell-1217 | 1.59.0 |
| chromium_headless_shell-1169 | 1.52.0–1.54.0 |
| chromium_headless_shell-1155 | 1.50.0 |

**Rule:** the version in `WysiMd.Blazor.IntegrationTests.csproj` must match the Chromium revision already installed on your machine. If they don't match, update the csproj — do NOT install a new browser just to match an old package version.

Current versions in csproj (`tests/WysiMd.Blazor.IntegrationTests/WysiMd.Blazor.IntegrationTests.csproj`):
```xml
<PackageReference Include="Microsoft.Playwright" Version="1.59.0" />
<PackageReference Include="Microsoft.Playwright.MSTest" Version="1.59.0" />
```

Both `Microsoft.Playwright` and `Microsoft.Playwright.MSTest` must be the **same version**.

---

### 2. Sample app must be running

The Playwright tests hit the live sample app. Start it before running tests:

```powershell
# Terminal 1 — leave running
dotnet run --project samples/WysiMd.Blazor.Sample
```

The sample app defaults to `http://localhost:5100` when run without `--urls`. The tests also default to `http://localhost:5100` via `PlaywrightFixture.BaseUrl`.

To use a different port, set the env var before running tests:
```powershell
$env:WYSIMD_BASE_URL = "http://localhost:5000"
dotnet test tests/WysiMd.Blazor.IntegrationTests
```

---

## Running the tests

```powershell
# Terminal 2 (sample app must already be running in Terminal 1)
dotnet test tests/WysiMd.Blazor.IntegrationTests
```

Expected output: `Passed! - Failed: 0, Passed: 13`

---

## Troubleshooting

### `Executable doesn't exist at ...chromium_headless_shell-XXXX`

Version mismatch. Check what's installed:
```powershell
ls "$env:LOCALAPPDATA\ms-playwright" | Where-Object Name -like "chromium*"
```

Then update both Playwright package references in the csproj to the matching version (see table above).

### Tests time out or fail to connect

The sample app isn't running, or is on a different port. Verify:
```powershell
curl http://localhost:5100  # should return 200
```

If it's on a different port:
```powershell
$env:WYSIMD_BASE_URL = "http://localhost:<port>"
```

### CI

CI starts the sample app automatically and sets `WYSIMD_BASE_URL`. No manual steps needed for CI runs.
