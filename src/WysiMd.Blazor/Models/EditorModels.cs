namespace WysiMd.Blazor.Models;

/// <summary>
/// Represents a text selection within the editor.
/// </summary>
public class EditorSelection
{
    /// <summary>Start offset of the selection (inclusive).</summary>
    public int Start { get; set; }
    /// <summary>End offset of the selection (exclusive).</summary>
    public int End { get; set; }
    /// <summary>The currently selected text, or an empty string when nothing is selected.</summary>
    public string SelectedText { get; set; } = string.Empty;
    /// <summary>True when the selection spans at least one character.</summary>
    public bool HasSelection => End > Start;
}

/// <summary>
/// Internal toolbar button definition (built-in defaults).
/// </summary>
public class ToolbarItem
{
    /// <summary>Unique string identifier (e.g. "bold", "link").</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Accessible label used for aria-label when no icon is provided.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>SVG markup rendered inside the toolbar button.</summary>
    public string Icon { get; set; } = string.Empty;
    /// <summary>Tooltip text shown on hover.</summary>
    public string Tooltip { get; set; } = string.Empty;
    /// <summary>Visual variant of this toolbar entry.</summary>
    public ToolbarItemType Type { get; set; } = ToolbarItemType.Button;
    /// <summary>Keyboard shortcut label appended to the tooltip (e.g. "Ctrl+B").</summary>
    public string? KeyboardShortcut { get; set; }
    /// <summary>Whether this button is currently in an active/pressed state.</summary>
    public bool IsActive { get; set; }
}

/// <summary>Visual variant of a toolbar entry.</summary>
public enum ToolbarItemType
{
    /// <summary>A clickable action button.</summary>
    Button,
    /// <summary>A visual divider between button groups.</summary>
    Separator,
    /// <summary>A button that opens a dropdown panel.</summary>
    Dropdown
}

/// <summary>
/// Per-item customization applied on top of the built-in defaults.
/// Set only the properties you want to override; nulls are ignored.
/// </summary>
public class ToolbarItemOptions
{
    /// <summary>Hide this item from the toolbar entirely.</summary>
    public bool Hidden { get; set; }

    /// <summary>Override the SVG icon markup.</summary>
    public string? Icon { get; set; }

    /// <summary>Override the tooltip text.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Override the keyboard shortcut label shown in the tooltip.</summary>
    public string? KeyboardShortcut { get; set; }
}

/// <summary>
/// Configures the editor features.
/// </summary>
public class EditorOptions
{
    /// <summary>Show the formatting toolbar above the editor.</summary>
    public bool ShowToolbar { get; set; } = true;
    /// <summary>Show the status bar (word count, reading time) below the editor.</summary>
    public bool ShowStatusBar { get; set; } = true;
    /// <summary>The editing mode presented when the component first renders.</summary>
    public EditorMode DefaultMode { get; set; } = EditorMode.Visual;
    /// <summary>Enable browser spell-checking in the editable area.</summary>
    public bool SpellCheck { get; set; } = true;
    /// <summary>Placeholder text shown when the editor is empty.</summary>
    public string Placeholder { get; set; } = "Start writing...";
    /// <summary>Maximum allowed character count, or null for no limit.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Allow the user to edit the document file name in the toolbar.</summary>
    public bool AllowFileNameEditing { get; set; } = true;
    /// <summary>Ordered list of toolbar item ids (use "|" for separators) shown in the main row.</summary>
    public List<string> EnabledToolbarItems { get; set; } = [.. DefaultToolbarItems];
    /// <summary>Ordered list of item ids shown in the overflow panel.</summary>
    public List<string> OverflowItems { get; set; } = [.. DefaultOverflowItems];
    /// <summary>Fixed height of the editor (CSS value, e.g. "600px"). Null lets the editor size to content.</summary>
    public string? Height { get; set; }
    /// <summary>Minimum height of the editor (CSS value).</summary>
    public string MinHeight { get; set; } = "400px";

    /// <summary>
    /// Dark-mode flag. Bind to the host app's theme state for automatic sync.
    /// </summary>
    public bool IsDarkTheme { get; set; }

    /// <summary>
    /// Override the editor background in light mode (e.g. "#ffffff" or "var(--mud-palette-surface)").
    /// Leave null to use the built-in default.
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// Override the editor background in dark mode.
    /// Leave null to use the built-in default.
    /// </summary>
    public string? DarkBackground { get; set; }

    /// <summary>
    /// When true the editor content cannot be modified; toolbar formatting
    /// actions are disabled. Mode-toggle, theme-toggle and overflow-toggle
    /// remain functional.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Per-item overrides keyed by toolbar item id (e.g. "bold", "image").
    /// Use to hide, re-icon, or re-label any built-in toolbar button.
    /// </summary>
    public Dictionary<string, ToolbarItemOptions> ToolbarItemOverrides { get; set; } = [];

    /// <summary>Default ordered item ids for the main toolbar row.</summary>
    public static readonly List<string> DefaultToolbarItems =
    [
        "heading", "|",
        "bold", "italic", "strikethrough", "|",
        "unordered-list", "ordered-list", "|",
        "link", "image", "|",
        "undo", "redo", "|",
        "mode-toggle", "theme-toggle", "overflow-toggle"
    ];

    /// <summary>Default ordered item ids for the overflow panel.</summary>
    public static readonly List<string> DefaultOverflowItems =
    [
        "code", "code-block", "blockquote", "horizontal-rule", "|",
        "task-list", "table", "|",
        "insert-row", "delete-row", "insert-col", "delete-col", "auto-sum", "|",
        "print", "download-pdf", "download-md"
    ];
}

/// <summary>Editing mode for the markdown editor.</summary>
public enum EditorMode
{
    /// <summary>WYSIWYG contenteditable view with live HTML rendering.</summary>
    Visual,
    /// <summary>Plain textarea showing raw Markdown source.</summary>
    Raw
}
