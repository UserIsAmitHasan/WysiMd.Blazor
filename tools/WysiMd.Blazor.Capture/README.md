# WysiMd.Blazor.Capture

Maintainer tool that regenerates the README demo GIF and screenshots in [`/assets`](../../assets).

It drives the **real sample app** with Playwright — every keystroke, toolbar click and dialog in
`demo.gif` happens in a live browser, so the assets can never show a UI the component doesn't have.

## Prerequisites

1. **ffmpeg** on `PATH` (GIF encoding). Without it the tool still records and prints the exact
   ffmpeg command to run by hand.
2. **Playwright's Chromium**, already installed if you have run the integration tests:
   ```powershell
   dotnet build tests/WysiMd.Blazor.IntegrationTests
   pwsh tests/WysiMd.Blazor.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
   ```
3. **The sample app running** — this tool captures it:
   ```powershell
   dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
   ```

## Usage

```powershell
# From the repository root, with the sample app running in another terminal
dotnet run --project tools/WysiMd.Blazor.Capture

# Screenshots only (fast — skips the ~35 s recording)
dotnet run --project tools/WysiMd.Blazor.Capture -- --only shots

# Against a sample app on a different port, writing elsewhere
dotnet run --project tools/WysiMd.Blazor.Capture -- --url http://localhost:5200 --out ./tmp
```

| Flag | Default | Meaning |
|---|---|---|
| `--url` | `http://localhost:5100` (or `WYSIMD_BASE_URL`) | Sample app base URL |
| `--out` | `<repo>/assets` | Output directory |
| `--only` | `both` | `video`, `shots`, or `both` |

## Output

| File | Capture |
|---|---|
| `demo.gif` | 760 px, ~20 s loop — type → H1 → bold → list → table dialog → Raw → dark → light |
| `editor-visual-light.png` | Visual mode, 1000 px stage, 2× DPI |
| `editor-visual-dark.png` | Same document, dark theme on a dark backdrop |
| `editor-raw.png` | Same document as Markdown source |
| `editor-mobile.png` | 390 × 844 viewport, 3× DPI |

## Notes

- **Do not change `GifWidth` without changing the README.** The GIF is embedded with no `width`
  attribute so it renders at its native size. Browsers resample *animated* images with a cheap
  filter, so a GIF displayed at any other size looks soft — which is why the recording is cropped
  to the editor (`GifCrop`) rather than scaled down further, and why the palette is 256 colours
  with `dither=none` (dither patterns smear as soon as the image is resampled).
- **Not in `WysiMd.Blazor.sln`** — CI never builds or restores it, so it costs nothing on PRs.
  The flip side is that it will not fail the build if it stops compiling; run it after UI changes.
- It depends on the component's **CSS class names and `data-action` ids** (`.wysimd-wysiwyg`,
  `[data-action='mode-toggle']`, …). Renaming those means updating this tool.
- The stage CSS in `Program.cs` hides the sample app's nav so only the editor is captured, and
  works around the `.wysimd-body` flex-grow gap noted in [ROADMAP.md](../../ROADMAP.md).
- Recordings land in `%TEMP%/wysimd-capture/` and are not cleaned up automatically — useful if you
  want an `.mp4` instead of a GIF.
- README images use absolute `raw.githubusercontent.com` URLs so the **NuGet package page** renders
  them too; relative paths would show as broken there.
