using Markdig;
using System.Text;
using System.Text.RegularExpressions;

namespace WysiMd.Blazor.Services;

/// <summary>
/// Core service for all Markdown operations. Handles parsing, rendering,
/// and text transformation entirely in C# without JavaScript.
/// </summary>
public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    /// <summary>Initialises the service and configures the Markdig pipeline.</summary>
    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseTaskLists()
            .UseSoftlineBreakAsHardlineBreak()
            .UsePipeTables()
            .Build();
    }

    // -----------------------------------------------------------------------
    // Rendering
    // -----------------------------------------------------------------------

    /// <summary>Converts markdown to safe HTML for preview rendering.</summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;
        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    // -----------------------------------------------------------------------
    // Inline Formatting Toggles
    // -----------------------------------------------------------------------

    /// <summary>Toggle **bold** around selection or word at cursor.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleBold(
        string markdown, int selStart, int selEnd)
        => ToggleInlineWrapper(markdown, selStart, selEnd, "**");

    /// <summary>Toggle *italic* around selection.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleItalic(
        string markdown, int selStart, int selEnd)
        => ToggleInlineWrapper(markdown, selStart, selEnd, "*");

    /// <summary>Toggle ~~strikethrough~~ around selection.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleStrikethrough(
        string markdown, int selStart, int selEnd)
        => ToggleInlineWrapper(markdown, selStart, selEnd, "~~");

    /// <summary>Toggle `inline code` around selection.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleInlineCode(
        string markdown, int selStart, int selEnd)
        => ToggleInlineWrapper(markdown, selStart, selEnd, "`");

    /// <summary>Insert a fenced code block at cursor, or wrap selection.</summary>
    public (string newMarkdown, int newCursor) InsertCodeBlock(
        string markdown, int selStart, int selEnd, string language = "")
    {
        string selected = selStart < selEnd ? markdown[selStart..selEnd] : string.Empty;
        string fence = $"```{language}\n{(selected.Length > 0 ? selected : "code")}\n```";

        // Ensure blank line before
        string before = markdown[..selStart];
        if (before.Length > 0 && !before.EndsWith("\n\n"))
            fence = (before.EndsWith('\n') ? "\n" : "\n\n") + fence;

        string result = markdown[..selStart] + fence + "\n\n" + markdown[selEnd..];
        int cursor = selStart + fence.IndexOf('\n') + 1; // land inside fence
        return (result, cursor);
    }

    private (string, int, int) ToggleInlineWrapper(
        string markdown, int selStart, int selEnd, string wrapper)
    {
        // Auto-expand to word if no selection
        if (selStart == selEnd)
            (selStart, selEnd) = ExpandToWord(markdown, selStart);

        string selected = markdown[selStart..selEnd];

        // If selection has leading/trailing spaces, shrink the selection to exclude them
        int originalStart = selStart;
        while (selStart < selEnd && char.IsWhiteSpace(markdown[selStart])) selStart++;
        while (selEnd > selStart && char.IsWhiteSpace(markdown[selEnd - 1])) selEnd--;

        if (selStart == selEnd) // Selection was just whitespace
        {
            string result2 = markdown[..originalStart] + wrapper + wrapper + markdown[originalStart..];
            return (result2, originalStart + wrapper.Length, originalStart + wrapper.Length);
        }

        selected = markdown[selStart..selEnd];

        // Check if already wrapped
        if (selected.StartsWith(wrapper) && selected.EndsWith(wrapper)
            && selected.Length > wrapper.Length * 2)
        {
            // Unwrap
            string unwrapped = selected[wrapper.Length..(selected.Length - wrapper.Length)];
            string result = markdown[..selStart] + unwrapped + markdown[selEnd..];
            return (result, selStart, selStart + unwrapped.Length);
        }

        // Check if surrounding context is wrapped
        int wLen = wrapper.Length;
        if (selStart >= wLen && selEnd + wLen <= markdown.Length)
        {
            string before = markdown[(selStart - wLen)..selStart];
            string after = markdown[selEnd..(selEnd + wLen)];
            if (before == wrapper && after == wrapper)
            {
                // Remove surrounding wrappers
                string result = markdown[..(selStart - wLen)]
                    + selected
                    + markdown[(selEnd + wLen)..];
                return (result, selStart - wLen, selEnd - wLen);
            }
        }

        // Wrap
        {
            string wrapped = wrapper + selected + wrapper;
            string result = markdown[..selStart] + wrapped + markdown[selEnd..];
            return (result, selStart + wLen, selEnd + wLen);
        }
    }

    // -----------------------------------------------------------------------
    // Block Formatting
    // -----------------------------------------------------------------------

    /// <summary>Set or toggle heading level (1-6) on the current line.</summary>
    public (string newMarkdown, int newCursor) SetHeading(
        string markdown, int cursorPos, int level)
    {
        (int lineStart, int lineEnd) = GetLineRange(markdown, cursorPos);
        string line = markdown[lineStart..lineEnd];

        // Strip existing heading prefix
        string stripped = Regex.Replace(line, @"^#{1,6}\s*", "");

        string prefix = level > 0 ? new string('#', level) + " " : "";

        // If same level already, remove it
        string existingPrefix = Regex.Match(line, @"^(#{1,6})\s").Groups[1].Value;
        if (existingPrefix.Length == level)
            prefix = "";

        string newLine = prefix + stripped;
        string result = markdown[..lineStart] + newLine + markdown[lineEnd..];
        int newCursor = lineStart + prefix.Length + stripped.Length;
        return (result, newCursor);
    }

    /// <summary>Toggle - unordered list on selected lines.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleUnorderedList(
        string markdown, int selStart, int selEnd)
        => ToggleLinePrefix(markdown, selStart, selEnd, "- ");

    /// <summary>Toggle numbered list on selected lines.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleOrderedList(
        string markdown, int selStart, int selEnd)
    {
        var lines = GetSelectedLines(markdown, selStart, selEnd);
        bool allOrdered = lines.All(l => Regex.IsMatch(l.Content, @"^\d+\.\s"));

        int lineStart = lines.First().Start;
        int lineEnd = lines.Last().End;

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (allOrdered)
                sb.Append(Regex.Replace(lines[i].Content, @"^\d+\.\s*", "")).Append('\n');
            else
                sb.Append($"{i + 1}. " + Regex.Replace(lines[i].Content, @"^\d+\.\s*", "")).Append('\n');
        }

        string newBlock = sb.ToString().TrimEnd('\n', '\r');
        string result = markdown[..lineStart] + newBlock + markdown[lineEnd..];
        return (result, lineStart, lineStart + newBlock.Length);
    }

    /// <summary>Toggle - [ ] task list on selected lines.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleTaskList(
        string markdown, int selStart, int selEnd)
        => ToggleLinePrefix(markdown, selStart, selEnd, "- [ ] ");

    /// <summary>Toggle blockquote > prefix on selected lines.</summary>
    public (string newMarkdown, int newStart, int newEnd) ToggleBlockquote(
        string markdown, int selStart, int selEnd)
        => ToggleLinePrefix(markdown, selStart, selEnd, "> ");

    /// <summary>Insert [text](url) link at selection.</summary>
    public (string newMarkdown, int newStart, int newEnd) InsertLink(
        string markdown, int selStart, int selEnd, string url, string text)
    {
        string linkMd = $"[{text}]({url})";
        string result = markdown[..selStart] + linkMd + markdown[selEnd..];
        return (result, selStart, selStart + linkMd.Length);
    }

    /// <summary>Insert a horizontal rule at the cursor line.</summary>
    public (string newMarkdown, int newCursor) InsertHorizontalRule(
        string markdown, int cursorPos)
    {
        (_, int lineEnd) = GetLineRange(markdown, cursorPos);
        string insert = "\n\n---\n\n";
        string result = markdown[..lineEnd] + insert + markdown[lineEnd..];
        return (result, lineEnd + insert.Length);
    }

    // -----------------------------------------------------------------------
    // Table
    // -----------------------------------------------------------------------

    /// <summary>Generate a GFM table with the given dimensions.</summary>
    public string GenerateTable(int rows, int cols, string[][]? data = null)
    {
        var sb = new StringBuilder();

        // Header row
        sb.Append("|");
        for (int c = 0; c < cols; c++)
        {
            string cell = data != null && data.Length > 0 && data[0].Length > c
                ? data[0][c] : $"Header {c + 1}";
            sb.Append($" {cell} |");
        }
        sb.AppendLine();

        // Separator
        sb.Append("|");
        for (int c = 0; c < cols; c++)
            sb.Append(" --- |");
        sb.AppendLine();

        // Data rows
        for (int r = 1; r < rows; r++)
        {
            sb.Append("|");
            for (int c = 0; c < cols; c++)
            {
                string cell = data != null && data.Length > r && data[r].Length > c
                    ? data[r][c] : $"Cell {r},{c + 1}";
                sb.Append($" {cell} |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Statistics
    // -----------------------------------------------------------------------

    /// <summary>Computes word count, character count, line count, and estimated reading time for <paramref name="markdown"/>.</summary>
    public EditorStats GetStats(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new EditorStats();

        string plainText = Regex.Replace(markdown, @"[#*_~`>\[\]!\-=|]", " ");
        plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

        int wordCount = plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int charCount = markdown.Length;
        int lineCount = markdown.Split('\n').Length;
        int readTimeSeconds = (int)Math.Ceiling(wordCount / 200.0 * 60);

        return new EditorStats
        {
            WordCount = wordCount,
            CharCount = charCount,
            LineCount = lineCount,
            ReadingTimeSeconds = readTimeSeconds
        };
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private (string newMarkdown, int newStart, int newEnd) ToggleLinePrefix(
        string markdown, int selStart, int selEnd, string prefix)
    {
        var lines = GetSelectedLines(markdown, selStart, selEnd);
        bool allPrefixed = lines.All(l => l.Content.StartsWith(prefix));

        int blockStart = lines.First().Start;
        int blockEnd = lines.Last().End;

        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            if (allPrefixed)
                sb.Append(line.Content[prefix.Length..]).Append('\n');
            else
                sb.Append(prefix + line.Content).Append('\n');
        }

        string newBlock = sb.ToString().TrimEnd('\n', '\r');
        string result = markdown[..blockStart] + newBlock + markdown[blockEnd..];
        return (result, blockStart, blockStart + newBlock.Length);
    }

    private List<LineInfo> GetSelectedLines(string markdown, int selStart, int selEnd)
    {
        (int firstLineStart, _) = GetLineRange(markdown, selStart);
        (_, int lastLineEnd) = GetLineRange(markdown, selEnd == selStart ? selStart : selEnd - 1);

        var lines = new List<LineInfo>();
        int pos = firstLineStart;
        while (pos <= lastLineEnd && pos < markdown.Length)
        {
            (int ls, int le) = GetLineRange(markdown, pos);
            lines.Add(new LineInfo { Start = ls, End = le, Content = markdown[ls..le] });
            pos = le + 1;
        }
        return lines;
    }

    private (int start, int end) GetLineRange(string markdown, int pos)
    {
        if (pos > markdown.Length) pos = markdown.Length;
        int start = pos;
        while (start > 0 && markdown[start - 1] != '\n')
            start--;
        int end = pos;
        while (end < markdown.Length && markdown[end] != '\n')
            end++;
        return (start, end);
    }

    private (int start, int end) ExpandToWord(string text, int pos)
    {
        int start = pos;
        int end = pos;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
        return (start, end);
    }

    private class LineInfo
    {
        public int Start { get; set; }
        public int End { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

/// <summary>Snapshot of editor content statistics.</summary>
public class EditorStats
{
    /// <summary>Number of words in the document.</summary>
    public int WordCount { get; set; }
    /// <summary>Total character count including Markdown syntax.</summary>
    public int CharCount { get; set; }
    /// <summary>Number of lines in the document.</summary>
    public int LineCount { get; set; }
    /// <summary>Estimated reading time in seconds (assuming 200 wpm).</summary>
    public int ReadingTimeSeconds { get; set; }

    /// <summary>Human-readable reading time (e.g. "45s read" or "2m 30s read").</summary>
    public string ReadingTimeDisplay => ReadingTimeSeconds < 60
        ? $"{ReadingTimeSeconds}s read"
        : $"{ReadingTimeSeconds / 60}m {ReadingTimeSeconds % 60}s read";
}
