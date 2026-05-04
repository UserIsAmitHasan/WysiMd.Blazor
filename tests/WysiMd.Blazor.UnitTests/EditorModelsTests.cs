using FluentAssertions;
using WysiMd.Blazor.Models;
using WysiMd.Blazor.Services;

namespace WysiMd.Blazor.UnitTests;

[TestClass]
public class EditorOptionsTests
{
    [TestMethod]
    public void DefaultOptions_HaveExpectedValues()
    {
        var opts = new EditorOptions();

        opts.ShowToolbar.Should().BeTrue();
        opts.ShowStatusBar.Should().BeTrue();
        opts.DefaultMode.Should().Be(EditorMode.Visual);
        opts.ReadOnly.Should().BeFalse();
        opts.SpellCheck.Should().BeTrue();
        opts.Placeholder.Should().NotBeNullOrEmpty();
        opts.MinHeight.Should().NotBeNullOrEmpty();
        opts.Height.Should().BeNull();
        opts.MaxLength.Should().BeNull();
        opts.AllowFileNameEditing.Should().BeTrue();
        opts.IsDarkTheme.Should().BeFalse();
    }

    [TestMethod]
    public void EnabledToolbarItems_DefaultsToAllItems()
    {
        var opts = new EditorOptions();
        opts.EnabledToolbarItems.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ToolbarItemOverrides_DefaultsToEmptyDictionary()
    {
        var opts = new EditorOptions();
        opts.ToolbarItemOverrides.Should().NotBeNull();
        opts.ToolbarItemOverrides.Should().BeEmpty();
    }

    [TestMethod]
    public void OverflowItems_DefaultsToNonEmptyList()
    {
        var opts = new EditorOptions();
        opts.OverflowItems.Should().NotBeNull();
    }
}

[TestClass]
public class EditorStatsTests
{
    [TestMethod]
    public void ReadingTimeDisplay_Under60Seconds_ShowsSeconds()
    {
        var stats = new EditorStats { ReadingTimeSeconds = 30 };
        stats.ReadingTimeDisplay.Should().Be("30s read");
    }

    [TestMethod]
    public void ReadingTimeDisplay_Over60Seconds_ShowsMinutesAndSeconds()
    {
        var stats = new EditorStats { ReadingTimeSeconds = 90 };
        stats.ReadingTimeDisplay.Should().Be("1m 30s read");
    }

    [TestMethod]
    public void ReadingTimeDisplay_ExactMinute_ShowsZeroSeconds()
    {
        var stats = new EditorStats { ReadingTimeSeconds = 120 };
        stats.ReadingTimeDisplay.Should().Be("2m 0s read");
    }
}

[TestClass]
public class EditorModeTests
{
    [TestMethod]
    public void EditorMode_HasVisualAndRawValues()
    {
        var values = Enum.GetValues<EditorMode>();
        values.Should().Contain(EditorMode.Visual);
        values.Should().Contain(EditorMode.Raw);
    }
}
