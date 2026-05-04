# API Reference

## MarkdownService

`MarkdownService` can be used standalone (inject it directly) or called internally by the editor component.

### Registration

```csharp
// Registers MarkdownService as a singleton
builder.Services.AddWysiMdBlazor();

// Or register manually:
builder.Services.AddSingleton<MarkdownService>();
```

### Rendering

```csharp
string html = markdownService.ToHtml("# Hello\n\nWorld");
// Returns: "<h1>Hello</h1>\n<p>World</p>\n"
```

Uses Markdig with CommonMark + GFM extensions (tables, task lists, soft→hard line breaks).

### Inline Formatting

All inline formatting methods return `(string newMarkdown, int newStart, int newEnd)`. Pass the cursor/selection positions and apply the new positions back to the textarea.

```csharp
// Toggle bold — wraps selection in ** or removes existing **
var (md, s, e) = svc.ToggleBold(markdown, selStart, selEnd);

// Toggle italic
var (md, s, e) = svc.ToggleItalic(markdown, selStart, selEnd);

// Toggle ~~strikethrough~~
var (md, s, e) = svc.ToggleStrikethrough(markdown, selStart, selEnd);

// Toggle `inline code`
var (md, s, e) = svc.ToggleInlineCode(markdown, selStart, selEnd);
```

**Auto-expand:** When `selStart == selEnd` (cursor, no selection), the method expands to the word at the cursor automatically.

**Toggle semantics:** Calling the same method twice returns to the original text.

### Block Formatting

```csharp
// Set heading level 1-6; 0 removes any heading prefix; same level = toggle off
var (md, cursor) = svc.SetHeading(markdown, cursorPos, level: 2);

// Toggle "- " prefix on selected lines
var (md, s, e) = svc.ToggleUnorderedList(markdown, selStart, selEnd);

// Toggle "1. " numbering on selected lines
var (md, s, e) = svc.ToggleOrderedList(markdown, selStart, selEnd);

// Toggle "- [ ] " task list prefix
var (md, s, e) = svc.ToggleTaskList(markdown, selStart, selEnd);

// Toggle "> " blockquote prefix
var (md, s, e) = svc.ToggleBlockquote(markdown, selStart, selEnd);

// Insert "---" horizontal rule below cursor line
var (md, cursor) = svc.InsertHorizontalRule(markdown, cursorPos);
```

### Links and Code

```csharp
// Insert [text](url) at selection
var (md, s, e) = svc.InsertLink(markdown, selStart, selEnd, url, text);

// Insert fenced code block; wraps selection if any
var (md, cursor) = svc.InsertCodeBlock(markdown, selStart, selEnd, language: "csharp");
```

### Tables

```csharp
// Generate a 3×4 GFM table with placeholder cells
string table = svc.GenerateTable(rows: 3, cols: 4);

// Generate with provided data (first row = header)
string table = svc.GenerateTable(rows: 3, cols: 2, data: new[]
{
    new[] { "Name", "Score" },
    new[] { "Alice", "95" },
    new[] { "Bob", "87" },
});
```

### Statistics

```csharp
EditorStats stats = svc.GetStats(markdown);
// stats.WordCount         — number of words (markdown symbols stripped)
// stats.CharCount         — raw character count
// stats.LineCount         — number of lines
// stats.ReadingTimeSeconds — based on 200 words/min
// stats.ReadingTimeDisplay — formatted string, e.g. "2m 30s read"
```

---

## EditorOptions

See [Configuration](configuration.md) for full property reference.

---

## MarkdownDocument

Internal document state. Normally you do not interact with this directly; it is owned by `MarkdownEditor`. Documented here for contributors.

```csharp
var doc = new MarkdownDocument();

doc.RawMarkdown = "# Hello";   // Sets content AND pushes history snapshot
doc.SetContentSilent("# Hi");  // Updates content WITHOUT pushing history
doc.Undo();                     // Restore previous snapshot
doc.Redo();                     // Replay forward snapshot
bool canUndo = doc.CanUndo;
bool canRedo = doc.CanRedo;
DateTime modified = doc.LastModified;
```

History is capped at **50 entries**. Consecutive identical snapshots are deduplicated.

---

## EditorStats

```csharp
public class EditorStats
{
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public int LineCount { get; set; }
    public int ReadingTimeSeconds { get; set; }
    public string ReadingTimeDisplay { get; }   // "45s read" or "2m 15s read"
}
```

---

## ServiceCollectionExtensions

```csharp
// Registers MarkdownService as a singleton
services.AddWysiMdBlazor();
```
