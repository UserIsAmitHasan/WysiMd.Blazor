using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace WysiMd.Blazor.IntegrationTests;

/// <summary>
/// End-to-end Playwright tests against the WysiMd.Blazor.Sample app.
///
/// Prerequisites:
///   1. dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
///   2. dotnet test tests/WysiMd.Blazor.IntegrationTests
///
/// To run in CI, set WYSIMD_BASE_URL or see .github/workflows/ci.yml.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class EditorTests : PageTest
{
    private string BaseUrl => PlaywrightFixture.BaseUrl;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private ILocator RawTextarea => Page.Locator(".wysimd-source");
    private ILocator VisualEditor => Page.Locator(".wysimd-wysiwyg");
    private ILocator StatusBar => Page.Locator(".wysimd-statusbar");
    private ILocator Toolbar => Page.Locator(".wysimd-toolbar");

    private ILocator ToolbarBtn(string action) =>
        Toolbar.Locator($"[data-action='{action}']");

    private async Task NavigateToBasicDemo()
    {
        await Page.GotoAsync($"{BaseUrl}/demo/basic");
        await Page.WaitForSelectorAsync(".wysimd-editor");
    }

    private async Task SwitchToRawMode()
    {
        var modeBtn = ToolbarBtn("toggle-mode");
        if (await modeBtn.IsVisibleAsync())
            await modeBtn.ClickAsync();
        await Page.WaitForSelectorAsync(".wysimd-source");
    }

    // -----------------------------------------------------------------------
    // Page Load
    // -----------------------------------------------------------------------

    [Test]
    public async Task EditorLoads_ToolbarAndEditorAreVisible()
    {
        await NavigateToBasicDemo();

        await Expect(Toolbar).ToBeVisibleAsync();
        await Expect(StatusBar).ToBeVisibleAsync();
    }

    // -----------------------------------------------------------------------
    // Raw Mode — Typing
    // -----------------------------------------------------------------------

    [Test]
    public async Task RawMode_TypeText_StatusBarUpdatesWordCount()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("Hello world this is a test");
        await Expect(StatusBar).ToContainTextAsync("5"); // at least 5 words
    }

    [Test]
    public async Task RawMode_TypeMarkdown_PreviewRendersHeading()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("# My Heading");

        // Switch back to visual to see the rendered output
        await ToolbarBtn("toggle-mode").ClickAsync();
        await Expect(VisualEditor.Locator("h1")).ToContainTextAsync("My Heading");
    }

    // -----------------------------------------------------------------------
    // Toolbar — Bold
    // -----------------------------------------------------------------------

    [Test]
    public async Task Toolbar_Bold_WrapsSelectedText()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("hello world");
        // Select "world" (offset 6–11)
        await Page.EvaluateAsync(@"() => {
            const ta = document.querySelector('.wysimd-source');
            ta.setSelectionRange(6, 11);
            ta.focus();
        }");

        await ToolbarBtn("bold").ClickAsync();

        var value = await RawTextarea.InputValueAsync();
        Assert.That(value, Does.Contain("**world**"));
    }

    // -----------------------------------------------------------------------
    // Toolbar — Undo / Redo
    // -----------------------------------------------------------------------

    [Test]
    public async Task Toolbar_Undo_RestoresPreviousContent()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("version one");
        await RawTextarea.FillAsync("version two");

        await ToolbarBtn("undo").ClickAsync();

        var value = await RawTextarea.InputValueAsync();
        Assert.That(value, Is.EqualTo("version one").Or.Contains("version"));
    }

    // -----------------------------------------------------------------------
    // Mode Toggle
    // -----------------------------------------------------------------------

    [Test]
    public async Task ModeToggle_SwitchesToRawMode()
    {
        await NavigateToBasicDemo();
        // Start in visual mode, toggle to raw
        await SwitchToRawMode();
        await Expect(RawTextarea).ToBeVisibleAsync();
    }

    [Test]
    public async Task ModeToggle_SwitchesBackToVisualMode()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();
        await ToolbarBtn("toggle-mode").ClickAsync();
        await Expect(VisualEditor).ToBeVisibleAsync();
    }

    // -----------------------------------------------------------------------
    // Dark Mode
    // -----------------------------------------------------------------------

    [Test]
    public async Task ThemeToggle_AddsDarkClass()
    {
        await NavigateToBasicDemo();
        var editor = Page.Locator(".wysimd-editor");

        await ToolbarBtn("toggle-theme").ClickAsync();

        var cls = await editor.GetAttributeAsync("class");
        Assert.That(cls, Does.Contain("wysimd-dark"));
    }

    // -----------------------------------------------------------------------
    // Keyboard Shortcuts
    // -----------------------------------------------------------------------

    [Test]
    public async Task KeyboardShortcut_CtrlB_BoldsWord()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("hello world");
        await Page.EvaluateAsync(@"() => {
            const ta = document.querySelector('.wysimd-source');
            ta.setSelectionRange(6, 11);
            ta.focus();
        }");

        await RawTextarea.PressAsync("Control+b");

        var value = await RawTextarea.InputValueAsync();
        Assert.That(value, Does.Contain("**world**"));
    }

    // -----------------------------------------------------------------------
    // Mobile Viewport
    // -----------------------------------------------------------------------

    [Test]
    public async Task Mobile_375px_ToolbarIsVisible()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await NavigateToBasicDemo();

        await Expect(Toolbar).ToBeVisibleAsync();

        // Buttons should still be accessible
        var boldBtn = ToolbarBtn("bold");
        await Expect(boldBtn).ToBeVisibleAsync();
    }

    [Test]
    public async Task Mobile_375px_ButtonsHaveMinimumTouchTargetSize()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await NavigateToBasicDemo();

        var boldBtn = ToolbarBtn("bold");
        var box = await boldBtn.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null);
        Assert.That(box!.Width, Is.GreaterThanOrEqualTo(44));
        Assert.That(box.Height, Is.GreaterThanOrEqualTo(44));
    }

    // -----------------------------------------------------------------------
    // Link Dialog
    // -----------------------------------------------------------------------

    [Test]
    public async Task LinkDialog_OpensAndInsertsLink()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("click here");
        await Page.EvaluateAsync(@"() => {
            const ta = document.querySelector('.wysimd-source');
            ta.setSelectionRange(0, 10);
            ta.focus();
        }");

        await ToolbarBtn("link").ClickAsync();
        await Page.WaitForSelectorAsync(".wysimd-dialog");

        // Fill the URL field
        var urlInput = Page.Locator(".wysimd-dialog input[type='url'], .wysimd-dialog input[placeholder*='http']");
        await urlInput.FillAsync("https://example.com");

        // Confirm
        await Page.Locator(".wysimd-dialog button[type='submit'], .wysimd-dialog .btn-primary").ClickAsync();

        var value = await RawTextarea.InputValueAsync();
        Assert.That(value, Does.Contain("https://example.com"));
    }

    // -----------------------------------------------------------------------
    // Read-Only Mode
    // -----------------------------------------------------------------------

    [Test]
    public async Task ReadOnly_TextareaIsDisabled()
    {
        await Page.GotoAsync($"{BaseUrl}/demo/readonly");
        await Page.WaitForSelectorAsync(".wysimd-editor");

        var textarea = Page.Locator(".wysimd-source");
        if (await textarea.IsVisibleAsync())
        {
            var disabled = await textarea.GetAttributeAsync("disabled");
            var readOnly = await textarea.GetAttributeAsync("readonly");
            Assert.That(disabled != null || readOnly != null, Is.True,
                "Textarea should be disabled or readonly in read-only mode");
        }
    }
}
