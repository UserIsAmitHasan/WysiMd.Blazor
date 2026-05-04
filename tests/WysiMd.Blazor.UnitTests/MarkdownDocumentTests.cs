using FluentAssertions;
using WysiMd.Blazor.Models;

namespace WysiMd.Blazor.UnitTests;

public class MarkdownDocumentTests
{
    [Fact]
    public void NewDocument_IsEmpty()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown.Should().BeEmpty();
        doc.CanUndo.Should().BeFalse();
        doc.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void SetContent_PushesHistory()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";

        doc.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void Undo_RestoresPreviousContent()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";

        doc.Undo();
        doc.RawMarkdown.Should().Be("first");
    }

    [Fact]
    public void Redo_ReappliesContent()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";
        doc.Undo();
        doc.Redo();

        doc.RawMarkdown.Should().Be("second");
    }

    [Fact]
    public void SetContentSilent_DoesNotPushHistory()
    {
        var doc = new MarkdownDocument();
        doc.SetContentSilent("typed text");

        doc.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void SetContentSilent_UpdatesRawMarkdown()
    {
        var doc = new MarkdownDocument();
        doc.SetContentSilent("hello");
        doc.RawMarkdown.Should().Be("hello");
    }

    [Fact]
    public void NewEdit_ClearsRedoStack()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "a";
        doc.RawMarkdown = "b";
        doc.Undo();
        doc.CanRedo.Should().BeTrue();

        doc.RawMarkdown = "c";
        doc.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void LastModified_UpdatesOnChange()
    {
        var doc = new MarkdownDocument();
        var before = doc.LastModified;
        doc.RawMarkdown = "changed";
        doc.LastModified.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Undo_WithNoHistory_DoesNotThrow()
    {
        var doc = new MarkdownDocument();
        doc.Invoking(d => d.Undo()).Should().NotThrow();
    }

    [Fact]
    public void Redo_WithNoFuture_DoesNotThrow()
    {
        var doc = new MarkdownDocument();
        doc.Invoking(d => d.Redo()).Should().NotThrow();
    }
}
