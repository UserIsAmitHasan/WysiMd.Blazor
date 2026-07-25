using FluentAssertions;
using WysiMd.Blazor.Models;

namespace WysiMd.Blazor.UnitTests;

[TestClass]
public class MarkdownDocumentTests
{
    [TestMethod]
    public void NewDocument_IsEmpty()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown.Should().BeEmpty();
        doc.CanUndo.Should().BeFalse();
        doc.CanRedo.Should().BeFalse();
    }

    [TestMethod]
    public void SetContent_PushesHistory()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";

        doc.CanUndo.Should().BeTrue();
    }

    [TestMethod]
    public void Undo_RestoresPreviousContent()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";

        var entry = doc.Undo();
        entry.Should().NotBeNull();
        entry!.Value.Content.Should().Be("first");
        doc.RawMarkdown.Should().Be("first");
    }

    [TestMethod]
    public void Redo_ReappliesContent()
    {
        var doc = new MarkdownDocument();
        doc.RawMarkdown = "first";
        doc.RawMarkdown = "second";
        doc.Undo();
        var entry = doc.Redo();

        entry.Should().NotBeNull();
        entry!.Value.Content.Should().Be("second");
        doc.RawMarkdown.Should().Be("second");
    }

    [TestMethod]
    public void SetContentSilent_DoesNotPushHistory()
    {
        var doc = new MarkdownDocument();
        doc.SetContentSilent("typed text");

        doc.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void SetContentSilent_UpdatesRawMarkdown()
    {
        var doc = new MarkdownDocument();
        doc.SetContentSilent("hello");
        doc.RawMarkdown.Should().Be("hello");
    }

    [TestMethod]
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

    [TestMethod]
    public void LastModified_UpdatesOnChange()
    {
        var doc = new MarkdownDocument();
        var before = doc.LastModified;
        doc.RawMarkdown = "changed";
        doc.LastModified.Should().BeOnOrAfter(before);
    }

    [TestMethod]
    public void Undo_WithNoHistory_DoesNotThrow()
    {
        var doc = new MarkdownDocument();
        doc.Invoking(d => d.Undo()).Should().NotThrow();
        doc.Undo().Should().BeNull();
    }

    [TestMethod]
    public void Redo_WithNoFuture_DoesNotThrow()
    {
        var doc = new MarkdownDocument();
        doc.Invoking(d => d.Redo()).Should().NotThrow();
        doc.Redo().Should().BeNull();
    }

    [TestMethod]
    public void PushHistory_PreservesCaretOffsets()
    {
        var doc = new MarkdownDocument("hello");
        doc.PushHistory(1, 3);
        doc.SetContentSilent("hello!");

        var entry = doc.Undo();
        entry.Should().NotBeNull();
        entry!.Value.CaretStart.Should().Be(1);
        entry.Value.CaretEnd.Should().Be(3);
    }
}
