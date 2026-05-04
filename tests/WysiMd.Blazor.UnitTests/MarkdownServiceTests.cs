using FluentAssertions;
using WysiMd.Blazor.Services;

namespace WysiMd.Blazor.UnitTests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _svc = new();

    // -----------------------------------------------------------------------
    // ToHtml
    // -----------------------------------------------------------------------

    [Fact]
    public void ToHtml_EmptyString_ReturnsEmpty()
    {
        _svc.ToHtml(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void ToHtml_Heading_RendersH1()
    {
        var html = _svc.ToHtml("# Hello");
        html.Should().Contain("<h1").And.Contain(">Hello</h1>");
    }

    [Fact]
    public void ToHtml_Bold_RendersBoldTag()
    {
        var html = _svc.ToHtml("**bold**");
        html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void ToHtml_GfmTable_RendersTableElement()
    {
        var md = "| A | B |\n| --- | --- |\n| 1 | 2 |";
        _svc.ToHtml(md).Should().Contain("<table>");
    }

    [Fact]
    public void ToHtml_TaskList_RendersCheckbox()
    {
        var html = _svc.ToHtml("- [ ] todo");
        html.Should().Contain("type=\"checkbox\"");
    }

    // -----------------------------------------------------------------------
    // ToggleBold
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleBold_WrapsSelection()
    {
        var (md, s, e) = _svc.ToggleBold("hello world", 6, 11);
        md.Should().Be("hello **world**");
        s.Should().Be(8);
        e.Should().Be(13);
    }

    [Fact]
    public void ToggleBold_UnwrapsExistingBold()
    {
        var (md, s, e) = _svc.ToggleBold("hello **world**", 8, 13);
        md.Should().Be("hello world");
        s.Should().Be(6);
        e.Should().Be(11);
    }

    [Fact]
    public void ToggleBold_CursorOnly_ExpandsToWord()
    {
        var (md, _, _) = _svc.ToggleBold("hello world", 7, 7);
        md.Should().Be("hello **world**");
    }

    [Fact]
    public void ToggleBold_EmptyString_InsertsMarkers()
    {
        var (md, _, _) = _svc.ToggleBold(string.Empty, 0, 0);
        md.Should().Be("****");
    }

    // -----------------------------------------------------------------------
    // ToggleItalic
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleItalic_WrapsSelection()
    {
        var (md, _, _) = _svc.ToggleItalic("test", 0, 4);
        md.Should().Be("*test*");
    }

    [Fact]
    public void ToggleItalic_UnwrapsExistingItalic()
    {
        var (md, _, _) = _svc.ToggleItalic("*test*", 1, 5);
        md.Should().Be("test");
    }

    // -----------------------------------------------------------------------
    // ToggleStrikethrough
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleStrikethrough_WrapsSelection()
    {
        var (md, _, _) = _svc.ToggleStrikethrough("delete me", 0, 9);
        md.Should().Be("~~delete me~~");
    }

    [Fact]
    public void ToggleStrikethrough_Unwraps()
    {
        var (md, _, _) = _svc.ToggleStrikethrough("~~delete me~~", 2, 11);
        md.Should().Be("delete me");
    }

    // -----------------------------------------------------------------------
    // ToggleInlineCode
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleInlineCode_WrapsSelection()
    {
        var (md, _, _) = _svc.ToggleInlineCode("var x = 1", 0, 9);
        md.Should().Be("`var x = 1`");
    }

    [Fact]
    public void ToggleInlineCode_Unwraps()
    {
        var (md, _, _) = _svc.ToggleInlineCode("`var x = 1`", 1, 10);
        md.Should().Be("var x = 1");
    }

    // -----------------------------------------------------------------------
    // SetHeading
    // -----------------------------------------------------------------------

    [Fact]
    public void SetHeading_AddsPrefix()
    {
        var (md, _) = _svc.SetHeading("Hello", 0, 2);
        md.Should().Be("## Hello");
    }

    [Fact]
    public void SetHeading_ReplacesExistingPrefix()
    {
        var (md, _) = _svc.SetHeading("# Hello", 0, 3);
        md.Should().Be("### Hello");
    }

    [Fact]
    public void SetHeading_SameLevel_Removes()
    {
        var (md, _) = _svc.SetHeading("## Hello", 0, 2);
        md.Should().Be("Hello");
    }

    [Fact]
    public void SetHeading_Level0_RemovesHeading()
    {
        var (md, _) = _svc.SetHeading("## Hello", 0, 0);
        md.Should().Be("Hello");
    }

    // -----------------------------------------------------------------------
    // ToggleUnorderedList
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleUnorderedList_AddsBullets()
    {
        var (md, _, _) = _svc.ToggleUnorderedList("line one\nline two", 0, 17);
        md.Should().Be("- line one\n- line two");
    }

    [Fact]
    public void ToggleUnorderedList_RemovesBullets()
    {
        var (md, _, _) = _svc.ToggleUnorderedList("- line one\n- line two", 0, 21);
        md.Should().Be("line one\nline two");
    }

    // -----------------------------------------------------------------------
    // ToggleOrderedList
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleOrderedList_NumbersLines()
    {
        var (md, _, _) = _svc.ToggleOrderedList("a\nb\nc", 0, 5);
        md.Should().Be("1. a\n2. b\n3. c");
    }

    [Fact]
    public void ToggleOrderedList_RemovesNumbers()
    {
        var (md, _, _) = _svc.ToggleOrderedList("1. a\n2. b", 0, 9);
        md.Should().Be("a\nb");
    }

    // -----------------------------------------------------------------------
    // ToggleTaskList
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleTaskList_AddsTaskPrefix()
    {
        var (md, _, _) = _svc.ToggleTaskList("todo item", 0, 9);
        md.Should().Be("- [ ] todo item");
    }

    // -----------------------------------------------------------------------
    // ToggleBlockquote
    // -----------------------------------------------------------------------

    [Fact]
    public void ToggleBlockquote_AddsPrefix()
    {
        var (md, _, _) = _svc.ToggleBlockquote("quote", 0, 5);
        md.Should().Be("> quote");
    }

    [Fact]
    public void ToggleBlockquote_Removes()
    {
        var (md, _, _) = _svc.ToggleBlockquote("> quote", 2, 7);
        md.Should().Be("quote");
    }

    // -----------------------------------------------------------------------
    // InsertLink
    // -----------------------------------------------------------------------

    [Fact]
    public void InsertLink_InsertsMarkdownLink()
    {
        var (md, s, e) = _svc.InsertLink("click here", 0, 10, "https://example.com", "click here");
        md.Should().Be("[click here](https://example.com)");
        s.Should().Be(0);
        e.Should().Be(md.Length);
    }

    // -----------------------------------------------------------------------
    // InsertHorizontalRule
    // -----------------------------------------------------------------------

    [Fact]
    public void InsertHorizontalRule_InsertsHr()
    {
        var (md, _) = _svc.InsertHorizontalRule("before", 6);
        md.Should().Contain("\n\n---\n\n");
    }

    // -----------------------------------------------------------------------
    // InsertCodeBlock
    // -----------------------------------------------------------------------

    [Fact]
    public void InsertCodeBlock_InsertsEmptyFence()
    {
        var (md, _) = _svc.InsertCodeBlock(string.Empty, 0, 0);
        md.Should().Contain("```");
        md.Should().Contain("code");
    }

    [Fact]
    public void InsertCodeBlock_WrapsSelection()
    {
        var (md, _) = _svc.InsertCodeBlock("let x = 1", 0, 9, "javascript");
        md.Should().Contain("```javascript");
        md.Should().Contain("let x = 1");
    }

    // -----------------------------------------------------------------------
    // GenerateTable
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateTable_ProducesCorrectColumnCount()
    {
        var table = _svc.GenerateTable(3, 4);
        var headerRow = table.Split('\n').First();
        headerRow.Split('|').Where(c => !string.IsNullOrWhiteSpace(c)).Should().HaveCount(4);
    }

    [Fact]
    public void GenerateTable_ProducesCorrectRowCount()
    {
        var table = _svc.GenerateTable(4, 2);
        // header + separator + 3 data rows = 5 non-empty lines
        var lines = table.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l));
        lines.Should().HaveCount(5);
    }

    [Fact]
    public void GenerateTable_WithData_UsesProvidedValues()
    {
        var table = _svc.GenerateTable(2, 2, new[] {
            new[] { "Name", "Score" },
            new[] { "Alice", "95" }
        });
        table.Should().Contain("Name");
        table.Should().Contain("Alice");
        table.Should().Contain("95");
    }

    // -----------------------------------------------------------------------
    // GetStats
    // -----------------------------------------------------------------------

    [Fact]
    public void GetStats_EmptyInput_ReturnsZeros()
    {
        var stats = _svc.GetStats(string.Empty);
        stats.WordCount.Should().Be(0);
        stats.CharCount.Should().Be(0);
        stats.LineCount.Should().Be(0);
    }

    [Fact]
    public void GetStats_CountsWords()
    {
        var stats = _svc.GetStats("one two three");
        stats.WordCount.Should().Be(3);
    }

    [Fact]
    public void GetStats_CountsChars()
    {
        var stats = _svc.GetStats("abc");
        stats.CharCount.Should().Be(3);
    }

    [Fact]
    public void GetStats_CountsLines()
    {
        var stats = _svc.GetStats("line1\nline2\nline3");
        stats.LineCount.Should().Be(3);
    }

    [Fact]
    public void GetStats_ReadingTimeDisplay_ShortText()
    {
        var stats = _svc.GetStats("word");
        stats.ReadingTimeDisplay.Should().EndWith("read");
    }

    [Fact]
    public void GetStats_ReadingTimeDisplay_LongText()
    {
        // 400 words → 2 min at 200 wpm
        var text = string.Join(" ", Enumerable.Repeat("word", 400));
        var stats = _svc.GetStats(text);
        stats.ReadingTimeDisplay.Should().StartWith("2m");
    }
}
