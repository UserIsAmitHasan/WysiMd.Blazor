namespace WysiMd.Blazor.Models;

/// <summary>
/// Represents the internal state of the markdown editor.
/// </summary>
public class MarkdownDocument
{
    private string _rawMarkdown = string.Empty;
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private const int MaxHistory = 50;

    /// <summary>The raw Markdown source. Setting this value updates history and <see cref="LastModified"/>.</summary>
    public string RawMarkdown
    {
        get => _rawMarkdown;
        set 
        {
            if (_rawMarkdown != value)
            {
                PushHistory();
                _rawMarkdown = value;
                _redoStack.Clear();
                LastModified = DateTime.UtcNow;
            }
        }
    }

    /// <summary>UTC timestamp of the last content change.</summary>
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;

    /// <summary>True when at least one undo step is available.</summary>
    public bool CanUndo => _undoStack.Count > 0;
    /// <summary>True when at least one redo step is available.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Initialises a new document with the given content (default: empty).</summary>
    public MarkdownDocument(string initialContent = "")
    {
        _rawMarkdown = initialContent;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>Saves the current content as an undo checkpoint (capped at 50 entries).</summary>
    public void PushHistory()
    {
        // Don't push duplicates
        if (_undoStack.Count > 0 && _undoStack.Peek() == _rawMarkdown) return;
        
        _undoStack.Push(_rawMarkdown);
        if (_undoStack.Count > MaxHistory)
        {
            // Simple way to keep stack size limited (optional: use a custom Circular Buffer for better perf)
            var list = _undoStack.ToList();
            list.RemoveAt(list.Count - 1);
            _undoStack.Clear();
            for (int i = list.Count - 1; i >= 0; i--) _undoStack.Push(list[i]);
        }
    }

    /// <summary>Reverts to the previous undo checkpoint. Returns false when nothing to undo.</summary>
    public bool Undo()
    {
        if (!CanUndo) return false;
        _redoStack.Push(_rawMarkdown);
        _rawMarkdown = _undoStack.Pop();
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>Re-applies the next redo checkpoint. Returns false when nothing to redo.</summary>
    public bool Redo()
    {
        if (!CanRedo) return false;
        _undoStack.Push(_rawMarkdown);
        _rawMarkdown = _redoStack.Pop();
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Directly set content without adding to history (used for live syncing).
    /// </summary>
    public void SetContentSilent(string content)
    {
        _rawMarkdown = content;
        LastModified = DateTime.UtcNow;
    }
}
