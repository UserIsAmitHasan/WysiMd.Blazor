using System.Diagnostics;
using System.Globalization;
using Microsoft.Playwright;

// ---------------------------------------------------------------------------
// Regenerates /assets/demo.gif and the README screenshots by driving the real
// sample app with Playwright. Nothing here is mocked — every keystroke and
// toolbar click happens in a live browser against samples/WysiMd.Blazor.Sample.
//
//   dotnet run --project tools/WysiMd.Blazor.Capture
//   dotnet run --project tools/WysiMd.Blazor.Capture -- --only shots
//
// See README.md in this folder for prerequisites.
// ---------------------------------------------------------------------------

// GIF encoding knobs — raising width or fps grows the file roughly linearly.
//
// Width matters for more than file size: browsers resample *animated* images
// with a cheap filter, so a GIF displayed at anything other than its native
// size looks soft. 760 px stays under GitHub's README column width, so it
// renders 1:1 there. The recording is cropped to the editor rather than scaled
// down further, which keeps the UI text the same size as a wider GIF would.
const double GifSpeed = 1.45;    // playback speed multiplier
const int GifFps = 12;
const int GifWidth = 760;
const int GifColors = 256;
const string GifCrop = "1060:640:70:40";   // w:h:x:y out of the 1200x720 recording

var baseUrl = Environment.GetEnvironmentVariable("WYSIMD_BASE_URL") ?? "http://localhost:5100";
var what = "both";
string? outDirArg = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--url" when i + 1 < args.Length: baseUrl = args[++i]; break;
        case "--out" when i + 1 < args.Length: outDirArg = args[++i]; break;
        case "--only" when i + 1 < args.Length: what = args[++i]; break;
        case "--help" or "-h":
            Console.WriteLine("usage: dotnet run -- [--url <base>] [--out <dir>] [--only video|shots|both]");
            return 0;
        default:
            Console.Error.WriteLine($"unknown argument: {args[i]}");
            return 2;
    }
}

if (what is not ("both" or "video" or "shots"))
{
    Console.Error.WriteLine($"--only must be video, shots or both (got '{what}')");
    return 2;
}

var outDir = outDirArg ?? Path.Combine(FindRepoRoot(), "assets");
Directory.CreateDirectory(outDir);

if (!await SampleAppIsRunningAsync(baseUrl))
{
    Console.Error.WriteLine($"""
        Could not reach the sample app at {baseUrl}.

        Start it first, in another terminal:
          dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
        """);
    return 1;
}

const string StageCss = """
    html, body {
        background: linear-gradient(140deg, #eef2ff 0%, #f8fafc 45%, #ecfeff 100%) !important;
        overflow: hidden !important;
    }
    .app-nav, .app-topbar, .nav-backdrop { display: none !important; }
    .app-shell { display: block !important; min-height: 100dvh !important; }
    .app-body { display: flex !important; align-items: center; justify-content: center; height: 100dvh; }
    .app-content {
        padding: 0 !important; margin: 0 !important; max-width: none !important; width: 100%;
        display: flex !important; align-items: center; justify-content: center;
    }
    .app-content > *:not(.demo-block) { display: none !important; }
    .demo-block {
        width: var(--stage-width, 940px) !important; margin: 0 !important; padding: 28px !important;
        border: none !important; background: transparent !important; box-shadow: none !important;
        border-radius: 0 !important;
    }
    .demo-block-header { display: none !important; }
    .wysimd-editor {
        border-radius: 12px !important;
        box-shadow: 0 20px 55px -14px rgba(15, 23, 42, .38), 0 0 0 1px rgba(15, 23, 42, .07) !important;
        overflow: hidden !important;
    }
    .wysimd-wysiwyg > *:first-child { margin-top: 0 !important; }
    /* Capture-only workaround: .wysimd-body has no flex-grow, so a document
       shorter than min-height leaves dead space under the status bar.
       See ROADMAP.md — "Editor body fills its min-height". */
    .wysimd-editor { height: var(--stage-editor-height, auto) !important; min-height: 0 !important; }
    .wysimd-body { flex: 1 1 auto !important; min-height: 0 !important; overflow: auto !important; }
    .wysimd-source { height: 100% !important; min-height: 0 !important; resize: none !important; }
    #__ripple {
        position: fixed; z-index: 2147483646; pointer-events: none; width: 34px; height: 34px;
        margin: -17px 0 0 -17px; border-radius: 50%; background: rgba(59, 130, 246, .35);
        opacity: 0; transform: scale(.35);
    }
    #__ripple.on { animation: __rp .5s ease-out; }
    @keyframes __rp {
        0%   { opacity: .85; transform: scale(.3); }
        100% { opacity: 0;   transform: scale(1.6); }
    }
    """;

// Playwright records no mouse pointer, so clicks would look like magic —
// this draws a synthetic cursor that glides to each target before clicking.
const string CursorJs = """
    () => {
        if (document.getElementById('__cursor')) return;
        const c = document.createElement('div');
        c.id = '__cursor';
        c.innerHTML = '<svg width="24" height="24" viewBox="0 0 24 24">'
            + '<path d="M5 2.2 L5 19.5 L9.6 15.2 L12.6 21.2 L15.4 19.8 L12.5 14.2 L18.8 13.9 Z" '
            + 'fill="#ffffff" stroke="#0f172a" stroke-width="1.4" stroke-linejoin="round"/></svg>';
        c.style.cssText = 'position:fixed;left:0;top:0;z-index:2147483647;pointer-events:none;'
            + 'transform:translate(-80px,-80px);transition:transform .38s cubic-bezier(.32,.72,.24,1);'
            + 'filter:drop-shadow(0 3px 4px rgba(15,23,42,.35));';
        document.body.appendChild(c);
        const r = document.createElement('div');
        r.id = '__ripple';
        document.body.appendChild(r);
    }
    """;

const string DemoMarkdown = """
    # Release Notes — v1.1.0

    **WysiMd.Blazor** is a WYSIWYG Markdown editor for Blazor. What you see is *always* clean Markdown.

    ## Highlights

    - [x] Visual and Raw editing modes
    - [x] GFM tables with row / column editing
    - [ ] Real-time collaboration

    | Feature | Status | Since |
    | --- | --- | --- |
    | Dark theme | Shipped | v1.0 |
    | Undo / redo | Shipped | v1.0 |
    | Debounced binding | Shipped | v1.1 |

    > Minimal JavaScript — every Markdown transform is pure C#.

    ```bash
    dotnet add package WysiMd.Blazor
    ```
    """;

const string MobileMarkdown = """
    # Release Notes

    **WysiMd.Blazor** is mobile-first — 48 px touch targets and bottom-sheet dialogs.

    - [x] Visual and Raw modes
    - [x] GFM tables
    - [ ] Real-time collaboration

    | Feature | Status |
    | --- | --- |
    | Dark theme | Shipped |
    | Undo / redo | Shipped |

    > Every Markdown transform is pure C#.
    """;

using var pw = await Playwright.CreateAsync();
await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,
    Channel = "chromium",
    Args = ["--force-color-profile=srgb", "--font-render-hinting=none", "--hide-scrollbars"],
});

if (what is "both" or "video") await RecordVideoAsync();
if (what is "both" or "shots") await CaptureScreenshotsAsync();

Console.WriteLine($"Assets written to {outDir}");
return 0;

// ---------------------------------------------------------------------------
// Stage helpers
// ---------------------------------------------------------------------------

// Hides the sample app's nav chrome and centres a single editor on a gradient
// backdrop, so the capture shows the component and nothing else.
async Task StageAsync(IPage page, string stageWidth = "940px", bool cursor = true, string editorHeight = "auto")
{
    await page.GotoAsync($"{baseUrl}/demo/basic");
    await page.WaitForSelectorAsync(".wysimd-editor");
    await page.WaitForTimeoutAsync(500);
    await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = StageCss });
    await page.EvaluateAsync("""
        () => {
            const r = document.documentElement.style;
            r.setProperty('--stage-width', '__W__');
            r.setProperty('--stage-editor-height', '__H__');
            document.querySelectorAll('.wysimd-wysiwyg, .wysimd-source')
                .forEach(e => e.setAttribute('spellcheck', 'false'));
        }
        """.Replace("__W__", stageWidth).Replace("__H__", editorHeight));
    if (cursor) await page.EvaluateAsync(CursorJs);
    await page.WaitForTimeoutAsync(300);
}

// Switching modes destroys and recreates the editor element, so Blazor's
// spellcheck="true" comes back with it — red squiggles under "WysiMd.Blazor"
// in the capture. Re-apply after anything that changes mode.
async Task DisableSpellcheckAsync(IPage page)
    => await page.EvaluateAsync(
        "() => document.querySelectorAll('.wysimd-wysiwyg, .wysimd-source').forEach(e => e.setAttribute('spellcheck', 'false'))");

async Task MoveCursorAsync(IPage page, double x, double y, int settle = 430)
{
    await page.EvaluateAsync(
        "([x, y]) => { const c = document.getElementById('__cursor'); if (c) c.style.transform = `translate(${x}px, ${y}px)`; }",
        new[] { x, y });
    await page.Mouse.MoveAsync((float)x, (float)y);
    await page.WaitForTimeoutAsync(settle);
}

async Task RippleAsync(IPage page, double x, double y)
{
    await page.EvaluateAsync("""
        ([x, y]) => {
            const r = document.getElementById('__ripple');
            if (!r) return;
            r.style.left = x + 'px';
            r.style.top = y + 'px';
            r.classList.remove('on');
            void r.offsetWidth;
            r.classList.add('on');
        }
        """, new[] { x, y });
}

async Task<(double X, double Y)> CenterAsync(ILocator loc)
{
    var box = await loc.BoundingBoxAsync() ?? throw new InvalidOperationException("element has no bounding box");
    return (box.X + box.Width / 2, box.Y + box.Height / 2);
}

async Task ClickElAsync(IPage page, ILocator loc, int after = 320)
{
    var (x, y) = await CenterAsync(loc);
    await MoveCursorAsync(page, x, y);
    await RippleAsync(page, x, y);
    await page.WaitForTimeoutAsync(120);
    await loc.ClickAsync();
    await page.WaitForTimeoutAsync(after);
}

async Task<bool> SelectPhraseAsync(IPage page, string phrase)
{
    return await page.EvaluateAsync<bool>("""
        (needle) => {
            const root = document.querySelector('.wysimd-wysiwyg');
            if (!root) return false;
            root.focus();
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            let n;
            while ((n = walker.nextNode())) {
                const i = n.textContent.indexOf(needle);
                if (i >= 0) {
                    const r = document.createRange();
                    r.setStart(n, i);
                    r.setEnd(n, i + needle.length);
                    const s = window.getSelection();
                    s.removeAllRanges();
                    s.addRange(r);
                    return true;
                }
            }
            return false;
        }
        """, phrase);
}

async Task TypeAsync(IPage page, string text, int delay = 42)
    => await page.Keyboard.TypeAsync(text, new KeyboardTypeOptions { Delay = delay });

// Rewrites the document through Raw mode — the only way to set content without
// leaving stray empty block elements behind in the contenteditable.
async Task SetMarkdownAsync(IPage page, string markdown)
{
    var modeBtn = page.Locator(".wysimd-toolbar [data-action='mode-toggle']").First;
    await modeBtn.ClickAsync();
    await page.Locator(".wysimd-source").WaitForAsync();
    await DisableSpellcheckAsync(page);
    await page.Locator(".wysimd-source").FillAsync(markdown);
    await page.WaitForTimeoutAsync(900);   // EditorOptions.DebounceDelay
    await modeBtn.ClickAsync();
    await page.Locator(".wysimd-wysiwyg").WaitForAsync();
    await page.WaitForTimeoutAsync(700);
    await DisableSpellcheckAsync(page);
}

// ---------------------------------------------------------------------------
// Video — scripted walkthrough, encoded to assets/demo.gif
// ---------------------------------------------------------------------------
async Task RecordVideoAsync()
{
    var videoDir = Path.Combine(Path.GetTempPath(), "wysimd-capture", Guid.NewGuid().ToString("N")[..8]);
    Directory.CreateDirectory(videoDir);

    var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new ViewportSize { Width = 1200, Height = 720 },
        RecordVideoDir = videoDir,
        RecordVideoSize = new RecordVideoSize { Width = 1200, Height = 720 },
    });

    var page = await ctx.NewPageAsync();
    var sw = Stopwatch.StartNew();

    await StageAsync(page, editorHeight: "520px");

    // Start from a genuinely empty document. Clearing with Ctrl+A/Delete in
    // visual mode leaves an empty <h1> behind, which shows up as a stray rule.
    await SetMarkdownAsync(page, string.Empty);
    var editor = page.Locator(".wysimd-wysiwyg");
    await page.WaitForTimeoutAsync(600);

    // Everything up to here is setup — trimmed off the front of the recording.
    var trimAt = sw.Elapsed.TotalSeconds;

    var toolbar = page.Locator(".wysimd-toolbar");
    ILocator Btn(string action) => toolbar.Locator($"[data-action='{action}']").First;

    await page.WaitForTimeoutAsync(700);

    // 1 — type a title, promote it to H1
    await editor.ClickAsync();
    await TypeAsync(page, "Release Notes", 55);
    await page.WaitForTimeoutAsync(400);

    var headingSelect = page.Locator(".wysimd-heading-select");
    var (hx, hy) = await CenterAsync(headingSelect);
    await MoveCursorAsync(page, hx, hy);
    await RippleAsync(page, hx, hy);
    await headingSelect.SelectOptionAsync("1");
    await page.WaitForTimeoutAsync(800);

    // 2 — a paragraph, then bold a phrase
    await editor.ClickAsync(new LocatorClickOptions { Position = new Position { X = 400, Y = 30 } });
    await page.Keyboard.PressAsync("Control+End");
    await page.Keyboard.PressAsync("Enter");
    await TypeAsync(page, "WysiMd.Blazor always writes clean Markdown.", 40);
    await page.WaitForTimeoutAsync(500);

    if (await SelectPhraseAsync(page, "clean Markdown"))
    {
        await page.WaitForTimeoutAsync(450);
        await ClickElAsync(page, Btn("bold"), 600);
    }

    // 3 — a bullet list
    await page.Keyboard.PressAsync("Control+End");
    await page.Keyboard.PressAsync("Enter");
    await ClickElAsync(page, Btn("unordered-list"), 350);
    await TypeAsync(page, "Visual and Raw modes", 40);
    await page.Keyboard.PressAsync("Enter");
    await TypeAsync(page, "Tables, images, undo", 40);
    await page.WaitForTimeoutAsync(500);
    await page.Keyboard.PressAsync("Enter");
    await page.Keyboard.PressAsync("Enter");   // leave the list
    await page.WaitForTimeoutAsync(400);

    // 4 — overflow panel → insert a table through the dialog
    await ClickElAsync(page, Btn("overflow-toggle"), 700);
    await ClickElAsync(page, Btn("table"), 500);
    var dialog = page.Locator(".wysimd-dialog");
    if (await dialog.IsVisibleAsync())
    {
        await page.WaitForTimeoutAsync(800);
        await ClickElAsync(page, dialog.Locator(".wysimd-btn-primary"), 900);
    }
    await ClickElAsync(page, Btn("overflow-toggle"), 600);

    // 5 — reveal the Markdown source
    await ClickElAsync(page, Btn("mode-toggle"), 400);
    await DisableSpellcheckAsync(page);
    await page.WaitForTimeoutAsync(2000);

    // 6 — dark theme
    await ClickElAsync(page, Btn("theme-toggle"), 1800);

    // 7 — back to visual, then back to light so the loop restarts cleanly
    await ClickElAsync(page, Btn("mode-toggle"), 400);
    await DisableSpellcheckAsync(page);
    await page.WaitForTimeoutAsync(1300);
    await ClickElAsync(page, Btn("theme-toggle"), 1100);

    await page.CloseAsync();
    await ctx.CloseAsync();   // the .webm is only flushed on context close

    var webm = Directory.GetFiles(videoDir, "*.webm").FirstOrDefault();
    if (webm is null)
    {
        Console.Error.WriteLine("no video was recorded");
        return;
    }

    await EncodeGifAsync(webm, trimAt, Path.Combine(outDir, "demo.gif"));
}

async Task EncodeGifAsync(string webm, double trimSeconds, string gifPath)
{
    // dither=none: Bayer/error-diffusion patterns are tuned for 1:1 viewing and
    // smear into visible noise as soon as the GIF is resampled. With a full
    // 256-colour palette, flat UI content needs no dithering — and it is smaller.
    var filter =
        $"[0:v]setpts=PTS/{GifSpeed.ToString(CultureInfo.InvariantCulture)},fps={GifFps}," +
        $"crop={GifCrop},scale={GifWidth}:-1:flags=lanczos,split[a][b];" +
        $"[a]palettegen=max_colors={GifColors}:stats_mode=diff[p];" +
        "[b][p]paletteuse=dither=none:diff_mode=rectangle";

    var psi = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true };
    foreach (var a in new[]
             {
                 "-v", "error", "-y",
                 "-ss", trimSeconds.ToString("F2", CultureInfo.InvariantCulture),
                 "-i", webm,
                 "-filter_complex", filter,
                 "-loop", "0", gifPath,
             })
    {
        psi.ArgumentList.Add(a);
    }

    try
    {
        using var p = Process.Start(psi)!;
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0)
        {
            Console.Error.WriteLine($"ffmpeg failed ({p.ExitCode}): {stderr}");
            return;
        }
        Console.WriteLine($"demo.gif  ({new FileInfo(gifPath).Length / 1024 / 1024.0:F1} MB)");
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
    {
        Console.WriteLine($"""
            ffmpeg was not found on PATH — the recording is kept at:
              {webm}

            Encode it manually with:
              ffmpeg -ss {trimSeconds.ToString("F2", CultureInfo.InvariantCulture)} -i "{webm}" -filter_complex "{filter}" -loop 0 "{gifPath}"
            """);
    }
}

// ---------------------------------------------------------------------------
// Screenshots
// ---------------------------------------------------------------------------
async Task SetStageDarkAsync(IPage page, bool dark)
{
    await page.EvaluateAsync("""
        (dark) => {
            let s = document.getElementById('__darkstage');
            if (!dark) { s?.remove(); return; }
            if (!s) {
                s = document.createElement('style');
                s.id = '__darkstage';
                document.head.appendChild(s);
            }
            s.textContent = 'html, body { background: linear-gradient(140deg,#0f172a 0%,#1e2438 45%,#0b1120 100%) !important; }'
                + '.wysimd-editor { box-shadow: 0 20px 55px -14px rgba(2,6,23,.75), 0 0 0 1px rgba(148,163,184,.16) !important; }';
        }
        """, dark);
    await page.WaitForTimeoutAsync(250);
}

async Task CaptureScreenshotsAsync()
{
    var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new ViewportSize { Width = 1240, Height = 1000 },
        DeviceScaleFactor = 2,
    });
    var page = await ctx.NewPageAsync();
    await StageAsync(page, "1000px", cursor: false);
    await SetMarkdownAsync(page, DemoMarkdown);

    var block = page.Locator(".demo-block");
    await block.ScreenshotAsync(new LocatorScreenshotOptions { Path = Path.Combine(outDir, "editor-visual-light.png") });

    var themeBtn = page.Locator(".wysimd-toolbar [data-action='theme-toggle']").First;
    await themeBtn.ClickAsync();
    await SetStageDarkAsync(page, true);
    await page.WaitForTimeoutAsync(600);
    await block.ScreenshotAsync(new LocatorScreenshotOptions { Path = Path.Combine(outDir, "editor-visual-dark.png") });

    await themeBtn.ClickAsync();
    await SetStageDarkAsync(page, false);
    await page.WaitForTimeoutAsync(400);
    await page.Locator(".wysimd-toolbar [data-action='mode-toggle']").First.ClickAsync();
    await page.Locator(".wysimd-source").WaitForAsync();
    // Tall enough to show the whole source document without trailing blank space.
    await page.EvaluateAsync("() => document.documentElement.style.setProperty('--stage-editor-height', '665px')");
    await page.EvaluateAsync("() => document.querySelectorAll('.wysimd-source').forEach(e => e.setAttribute('spellcheck','false'))");
    await page.WaitForTimeoutAsync(700);
    await block.ScreenshotAsync(new LocatorScreenshotOptions { Path = Path.Combine(outDir, "editor-raw.png") });
    await page.CloseAsync();
    await ctx.CloseAsync();

    var mctx = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        DeviceScaleFactor = 3,
    });
    var mpage = await mctx.NewPageAsync();
    await StageAsync(mpage, "390px", cursor: false);
    await mpage.AddStyleTagAsync(new PageAddStyleTagOptions
    {
        Content = ".demo-block { padding: 18px !important; } .app-body { height: auto !important; }",
    });
    await SetMarkdownAsync(mpage, MobileMarkdown);
    await mpage.EvaluateAsync("() => { const t = document.querySelector('.wysimd-toolbar'); if (t) t.scrollLeft = 0; }");
    await mpage.WaitForTimeoutAsync(300);
    await mpage.Locator(".demo-block").ScreenshotAsync(new LocatorScreenshotOptions
    {
        Path = Path.Combine(outDir, "editor-mobile.png"),
    });
    await mpage.CloseAsync();
    await mctx.CloseAsync();

    Console.WriteLine("editor-visual-light.png, editor-visual-dark.png, editor-raw.png, editor-mobile.png");
}

// ---------------------------------------------------------------------------

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Environment.CurrentDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "WysiMd.Blazor.sln"))) return dir.FullName;
        dir = dir.Parent;
    }
    return Environment.CurrentDirectory;
}

static async Task<bool> SampleAppIsRunningAsync(string url)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    try
    {
        var res = await http.GetAsync(url);
        return res.IsSuccessStatusCode;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        return false;
    }
}
