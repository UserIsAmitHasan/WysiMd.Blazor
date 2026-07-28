using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
[TestClass]
public class EditorTests : PageTest
{
    private string BaseUrl => PlaywrightFixture.BaseUrl;

    [TestInitialize]
    public void SetDefaultTimeout() => Page.SetDefaultTimeout(20_000);

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
        var modeBtn = ToolbarBtn("mode-toggle");
        await modeBtn.WaitForAsync();
        await modeBtn.ClickAsync();
        await RawTextarea.WaitForAsync();
    }

    // -----------------------------------------------------------------------
    // Page Load
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task EditorLoads_ToolbarAndEditorAreVisible()
    {
        await NavigateToBasicDemo();

        await Expect(Toolbar).ToBeVisibleAsync();
        await Expect(StatusBar).ToBeVisibleAsync();
    }

    // -----------------------------------------------------------------------
    // Raw Mode — Typing
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task RawMode_TypeText_StatusBarUpdatesWordCount()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("Hello world this is a test");
        await Expect(StatusBar).ToContainTextAsync("6 words");
    }

    [TestMethod]
    public async Task RawMode_TypeMarkdown_PreviewRendersHeading()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("# My Heading");

        // Switch back to visual to see the rendered output
        await ToolbarBtn("mode-toggle").ClickAsync();
        await Expect(VisualEditor.Locator("h1")).ToContainTextAsync("My Heading");
    }

    // -----------------------------------------------------------------------
    // Toolbar — Bold
    // -----------------------------------------------------------------------

    [TestMethod]
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
        StringAssert.Contains(value, "**world**");
    }

    // -----------------------------------------------------------------------
    // Toolbar — Undo / Redo
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task Toolbar_Undo_RestoresPreviousContent()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();

        await RawTextarea.FillAsync("version one");
        // Edits within 1 s coalesce into a single undo checkpoint — wait the
        // window out so "version one" becomes its own history entry.
        await Page.WaitForTimeoutAsync(1200);
        await RawTextarea.FillAsync("version two");

        await ToolbarBtn("undo").ClickAsync();
        var value = await RawTextarea.InputValueAsync();
        Assert.AreEqual("version one", value);

        await ToolbarBtn("redo").ClickAsync();
        value = await RawTextarea.InputValueAsync();
        Assert.AreEqual("version two", value);
    }

    // -----------------------------------------------------------------------
    // Mode Toggle
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task ModeToggle_SwitchesToRawMode()
    {
        await NavigateToBasicDemo();
        // Start in visual mode, toggle to raw
        await SwitchToRawMode();
        await Expect(RawTextarea).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ModeToggle_SwitchesBackToVisualMode()
    {
        await NavigateToBasicDemo();
        await SwitchToRawMode();
        await ToolbarBtn("mode-toggle").ClickAsync();
        await Expect(VisualEditor).ToBeVisibleAsync();
    }

    // -----------------------------------------------------------------------
    // Dark Mode
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task ThemeToggle_AddsDarkClass()
    {
        await NavigateToBasicDemo();
        var editor = Page.Locator(".wysimd-editor");

        await ToolbarBtn("theme-toggle").ClickAsync();

        var cls = await editor.GetAttributeAsync("class");
        StringAssert.Contains(cls, "wysimd-dark");
    }

    // -----------------------------------------------------------------------
    // Keyboard Shortcuts
    // -----------------------------------------------------------------------

    [TestMethod]
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
        StringAssert.Contains(value, "**world**");
    }

    // -----------------------------------------------------------------------
    // Mobile Viewport
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task Mobile_375px_ToolbarIsVisible()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await NavigateToBasicDemo();

        await Expect(Toolbar).ToBeVisibleAsync();

        // Buttons should still be accessible
        var boldBtn = ToolbarBtn("bold");
        await Expect(boldBtn).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Mobile_375px_ButtonsHaveMinimumTouchTargetSize()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await NavigateToBasicDemo();

        var boldBtn = ToolbarBtn("bold");
        var box = await boldBtn.BoundingBoxAsync();
        Assert.IsNotNull(box);
        Assert.IsTrue(box!.Width >= 44, $"Expected width >= 44 but was {box.Width}");
        Assert.IsTrue(box.Height >= 44, $"Expected height >= 44 but was {box.Height}");
    }

    // -----------------------------------------------------------------------
    // Link Dialog
    // -----------------------------------------------------------------------

    [TestMethod]
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
        await urlInput.PressAsync("Tab"); // commit @bind (onchange fires on blur)

        // Confirm
        await Page.Locator(".wysimd-dialog .wysimd-btn-primary").ClickAsync();

        var value = await RawTextarea.InputValueAsync();
        StringAssert.Contains(value, "https://example.com");
    }

    // -----------------------------------------------------------------------
    // Read-Only Mode
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task ReadOnly_TextareaIsDisabled()
    {
        await Page.GotoAsync($"{BaseUrl}/demo/readonly");
        await Page.WaitForSelectorAsync(".wysimd-editor");

        var textarea = Page.Locator(".wysimd-source");
        if (await textarea.IsVisibleAsync())
        {
            var disabled = await textarea.GetAttributeAsync("disabled");
            var readOnly = await textarea.GetAttributeAsync("readonly");
            Assert.IsTrue(disabled != null || readOnly != null,
                "Textarea should be disabled or readonly in read-only mode");
        }
    }
}
