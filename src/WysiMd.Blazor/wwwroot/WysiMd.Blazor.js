// WysiMdBlazor.js
// Minimal JS — only handles raw cursor/selection tracking in the textarea
// and a few DOM operations that have no C# equivalent.

window.WysiMdBlazor = {
    /**
     * Get the current selection start/end from a textarea.
     * Returns { start, end, value }
     */
    getSelection: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;
        return {
            start: el.selectionStart,
            end: el.selectionEnd,
            value: el.value
        };
    },

    /**
     * Set the selection range and focus the textarea.
     */
    setSelection: function (elementId, start, end) {
        const el = document.getElementById(elementId);
        if (!el) return;
        el.focus();
        el.setSelectionRange(start, end);
    },

    /**
     * Set textarea value and selection atomically.
     * Used after C# transforms the markdown to avoid double-render flicker.
     */
    setValueAndSelection: function (elementId, value, start, end) {
        const el = document.getElementById(elementId);
        if (!el) return;
        el.value = value;
        el.setSelectionRange(start, end);
        // Dispatch input event so Blazor picks up the new value
        el.dispatchEvent(new Event('input', { bubbles: true }));
    },

    /**
     * Auto-resize a textarea to fit its content.
     */
    autoResize: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;
        el.style.height = 'auto';
        el.style.height = el.scrollHeight + 'px';
    },

    /**
     * Trigger a click on an element (e.g. hidden file input).
     */
    clickElement: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) el.click();
    },

    /**
     * Download a file from a data URL or Blob.
     */
    downloadFile: function (filename, content) {
        const link = document.createElement('a');
        link.download = filename;
        link.href = content;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    /**
     * Register keyboard shortcut interceptor.
     */
    registerShortcuts: function (elementId, dotnetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        el.addEventListener('keydown', async function (e) {
            // Exit <pre> (code block) or <blockquote> on Enter on an empty line
            if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.metaKey) {
                const sel = window.getSelection();
                if (sel && sel.rangeCount > 0) {
                    const range = sel.getRangeAt(0);
                    const anchor = range.startContainer;

                    // Walk up to find if we're inside <pre> or <blockquote>
                    const exitTag = (tag) => {
                        let node = anchor.nodeType === 1 ? anchor : anchor.parentElement;
                        while (node && node !== el) {
                            if (node.tagName && node.tagName.toLowerCase() === tag) return node;
                            node = node.parentElement;
                        }
                        return null;
                    };

                    const preNode = exitTag('pre');
                    const bqNode = exitTag('blockquote');
                    const container = preNode || bqNode;

                    if (container) {
                        // Get text of current line
                        const lineText = anchor.nodeType === 3
                            ? anchor.textContent.slice(0, range.startOffset)
                            : '';
                        const isEmptyLine = lineText.trimEnd() === '' &&
                            (anchor.nodeType !== 3 || anchor.textContent.trim() === '');

                        if (isEmptyLine) {
                            e.preventDefault();

                            // Remove the empty line node (or text node) the cursor is on
                            const emptyNode = anchor.nodeType === 3 ? anchor : range.startContainer;
                            // For <pre>: trim trailing newline from the text content
                            if (preNode) {
                                const code = preNode.querySelector('code') || preNode;
                                code.textContent = code.textContent.replace(/\n\s*$/, '');
                            }
                            // For <blockquote>: remove the empty child node the cursor is in
                            if (bqNode) {
                                let nodeToRemove = anchor.nodeType === 3 ? anchor.parentElement : anchor;
                                if (nodeToRemove && nodeToRemove !== bqNode && bqNode.contains(nodeToRemove)) {
                                    nodeToRemove.remove();
                                }
                            }

                            // Insert a new <p> after the container and move cursor into it
                            const p = document.createElement('p');
                            p.innerHTML = '<br>';
                            container.parentNode.insertBefore(p, container.nextSibling);

                            // Move cursor to the new paragraph
                            const newRange = document.createRange();
                            newRange.setStart(p, 0);
                            newRange.collapse(true);
                            sel.removeAllRanges();
                            sel.addRange(newRange);

                            // Notify Blazor
                            await dotnetRef.invokeMethodAsync('HandleShortcut', 'sync-wysiwyg');
                            return;
                        }
                    }
                }
            }

            // 1. Handle Ctrl/Meta shortcuts
            if (e.ctrlKey || e.metaKey) {
                const key = e.key.toLowerCase();
                const shift = e.shiftKey;
                
                let action = null;
                if (shift) {
                    if (key === 'x') action = 'strikethrough';
                    if (key === '8' || key === '*') action = 'unordered-list';
                    if (key === '7' || key === '&') action = 'ordered-list';
                    if (key === 'b') action = 'blockquote';
                } else {
                    if (key === 'z') action = 'undo';
                    if (key === 'y') action = 'redo';
                    if (key === 'b') action = 'bold';
                    if (key === 'i') action = 'italic';
                    if (key === 's') action = 'download-md';
                    if (key === 'p') action = 'print';
                    if (key === 'k') action = 'image';
                    if (key === 'l') action = 'link';
                    if (key === '`') action = 'code';
                }
                
                if (action) {
                    e.preventDefault();
                    await dotnetRef.invokeMethodAsync('HandleShortcut', action);
                    return;
                }
            }
        });
    },
/**
 * WYSIWYG: Execute a rich text command.
 */
    /**
     * Save current selection in visual mode.
     */
    saveSelection: function () {
        const sel = window.getSelection();
        if (sel.rangeCount > 0) {
            this._savedSelection = sel.getRangeAt(0);
        }
    },

    /**
     * Restore saved selection in visual mode.
     */
    restoreSelection: function () {
        if (!this._savedSelection) return;
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(this._savedSelection);
    },

    /**
     * WYSIWYG: Execute a rich text command.
     * insertHorizontalRule is deprecated in modern browsers — use insertHTML instead.
     */
    execCommand: function (command, value = null) {
        if (command === 'insertHorizontalRule') {
            document.execCommand('insertHTML', false, '<hr>');
        } else {
            document.execCommand(command, false, value);
        }

        // For inline toggles with a collapsed selection, queryCommandState reflects the
        // inherited context and will snap the button back. Force a re-query after a tick
        // so the browser has settled the insertion-point style.
        const toggleCommands = ['bold', 'italic', 'strikethrough'];
        if (toggleCommands.includes(command) && this._selectionCallback) {
            setTimeout(this._selectionCallback, 0);
        }
    },

    /**
     * WYSIWYG: Wrap selection in a inline <code> tag.
     */
    insertInlineCode: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;
        const range = sel.getRangeAt(0);
        const selected = range.toString();
        const code = document.createElement('code');
        code.textContent = selected.length > 0 ? selected : 'code';
        range.deleteContents();
        range.insertNode(code);
        sel.collapse(code, 1);
    },

    /**
     * WYSIWYG: Insert a fenced code block as a <pre><code> element.
     */
    insertCodeBlock: function () {
        const pre = document.createElement('pre');
        const code = document.createElement('code');
        code.textContent = 'code';
        pre.appendChild(code);
        document.execCommand('insertHTML', false, pre.outerHTML + '<p><br></p>');
    },

    /**
     * WYSIWYG: Wrap selected paragraphs in a <blockquote>.
     */
    insertBlockquote: function () {
        document.execCommand('formatBlock', false, 'blockquote');
    },

    /**
     * WYSIWYG: Insert an anchor link around the selection.
     */
    insertLink: function (url, text) {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;
        const range = sel.getRangeAt(0);
        const selected = range.toString();
        const a = document.createElement('a');
        a.href = url;
        a.textContent = selected.length > 0 ? selected : text;
        range.deleteContents();
        range.insertNode(a);
        sel.collapse(a, 1);
    },

    /**
     * Helper to find parent by tag name.
     */
    _getParent: function (node, tagName) {
        if (!node) return null;
        let curr = node.nodeType === 1 ? node : node.parentElement;
        return curr ? curr.closest(tagName) : null;
    },

    /**
     * WYSIWYG: Insert a row below the current selection.
     */
    insertRow: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;
        
        const tr = WysiMdBlazor._getParent(sel.anchorNode, "tr");
        if (!tr) return;

        const table = tr.closest("table");
        if (!table) return;

        const newRow = table.insertRow(tr.rowIndex + 1);
        const cellCount = tr.cells.length;
        for (let i = 0; i < cellCount; i++) {
            const cell = newRow.insertCell(i);
            cell.innerHTML = "<br>";
        }
    },

    /**
     * WYSIWYG: Delete the current row.
     */
    deleteRow: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;
        
        const tr = WysiMdBlazor._getParent(sel.anchorNode, "tr");
        if (!tr) return;

        const table = tr.closest("table");
        if (!table) return;

        table.deleteRow(tr.rowIndex);
        
        if (table.rows.length === 0) {
            table.remove();
        }
    },

    /**
     * WYSIWYG: Delete the current column.
     */
    deleteColumn: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;

        const td = WysiMdBlazor._getParent(sel.anchorNode, "td") || WysiMdBlazor._getParent(sel.anchorNode, "th");
        if (!td) return;

        const table = td.closest("table");
        if (!table) return;

        const colIndex = td.cellIndex;
        for (let i = 0; i < table.rows.length; i++) {
            if (table.rows[i].cells.length > colIndex) {
                table.rows[i].deleteCell(colIndex);
            }
        }

        if (table.rows.length > 0 && table.rows[0].cells.length === 0) {
            table.remove();
        }
    },

    /**
     * WYSIWYG: Sum all numbers in the current column and put result in selected cell.
     */
    autoSum: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;

        const td = WysiMdBlazor._getParent(sel.anchorNode, "td") || WysiMdBlazor._getParent(sel.anchorNode, "th");
        if (!td) return;

        const table = td.closest("table");
        if (!table) return;

        const colIndex = td.cellIndex;
        let sum = 0;
        let allValid = true;
        let hasNumbers = false;

        // Iterate through rows, skipping the first (header)
        for (let i = 1; i < table.rows.length; i++) {
            const row = table.rows[i];
            if (row.cells.length > colIndex) {
                const cell = row.cells[colIndex];
                // Don't sum the cell we are currently in if we are writing the result there
                if (cell === td) continue;

                const val = cell.innerText.trim();
                if (val === "") continue; // Skip empty cells

                const num = parseFloat(val);
                if (isNaN(num)) {
                    allValid = false;
                    break;
                }
                sum += num;
                hasNumbers = true;
            }
        }

        if (allValid && hasNumbers) {
            td.innerText = sum.toString();
        }
    },

    /**
     * WYSIWYG: Insert a column to the right of the current selection.
     */
    insertColumn: function () {
        const sel = window.getSelection();
        if (!sel.rangeCount) return;

        const td = WysiMdBlazor._getParent(sel.anchorNode, "td") || WysiMdBlazor._getParent(sel.anchorNode, "th");
        if (!td) return;

        const table = td.closest("table");
        if (!table) return;

        const colIndex = td.cellIndex;
        for (let i = 0; i < table.rows.length; i++) {
            const row = table.rows[i];
            const isHeader = row.parentNode && row.parentNode.tagName.toLowerCase() === "thead";
            const newCell = document.createElement(isHeader ? "th" : "td");
            newCell.innerHTML = "<br>";
            row.insertBefore(newCell, row.cells[colIndex + 1]);
        }
    },

    /**
     * WYSIWYG: Register a selectionchange listener that fires active-format state back to Blazor.
     */
    registerSelectionListener: function (elementId, dotnetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        const query = () => {
            // Only fire when focus is inside this editor element
            const active = document.activeElement;
            if (!el.contains(active) && active !== el) return;

            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0) return;

            const anchor = sel.anchorNode;
            const closest = (tag) => {
                let node = anchor && anchor.nodeType === 1 ? anchor : anchor?.parentElement;
                while (node && node !== el) {
                    if (node.tagName?.toLowerCase() === tag) return true;
                    node = node.parentElement;
                }
                return false;
            };

            const formats = {
                bold: document.queryCommandState('bold'),
                italic: document.queryCommandState('italic'),
                strikethrough: document.queryCommandState('strikethrough'),
                unorderedList: document.queryCommandState('insertUnorderedList'),
                orderedList: document.queryCommandState('insertOrderedList'),
                codeBlock: closest('pre'),
                blockquote: closest('blockquote'),
                code: closest('code') && !closest('pre'),
            };

            dotnetRef.invokeMethodAsync('UpdateActiveFormats', formats);
        };

        document.addEventListener('selectionchange', query);
        el.addEventListener('keyup', query);

        // Make query available to execCommand for post-toggle re-query
        WysiMdBlazor._selectionCallback = query;

        el._selectionCleanup = () => {
            document.removeEventListener('selectionchange', query);
            el.removeEventListener('keyup', query);
            delete WysiMdBlazor._selectionCallback;
        };
    },

    unregisterSelectionListener: function (elementId) {
        const el = document.getElementById(elementId);
        if (el?._selectionCleanup) {
            el._selectionCleanup();
            delete el._selectionCleanup;
        }
    },

    /**
     * WYSIWYG: Get HTML from contenteditable and convert to accurate Markdown.
     * Uses a recursive DOM walker to handle nested structures correctly.
     */
    getMarkdownFromHtml: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return "";

        const walk = (node) => {
            if (node.nodeType === 3) { // Text node
                return node.nodeValue;
            }
            if (node.nodeType !== 1) { // Not an element
                return "";
            }

            const tag = node.tagName.toLowerCase();

            // Special handling for tables
            if (tag === "table") {
                const rows = Array.from(node.rows || node.querySelectorAll('tr'));
                if (rows.length === 0) return "";
                
                let tableMd = "\n";
                rows.forEach((row, rowIndex) => {
                    let rowMd = "|";
                    const cells = Array.from(row.cells || row.querySelectorAll('th, td'));
                    cells.forEach(cell => {
                        let cellContent = "";
                        for (let i = 0; i < cell.childNodes.length; i++) {
                            cellContent += walk(cell.childNodes[i]);
                        }
                        // GFM cells don't support real newlines, use <br> if needed
                        rowMd += " " + cellContent.trim().replace(/\n/g, "<br>") + " |";
                    });
                    tableMd += rowMd + "\n";
                    
                    if (rowIndex === 0) {
                        tableMd += "|" + cells.map(() => " --- ").join("|") + "|\n";
                    }
                });
                return tableMd + "\n";
            }

            let children = "";
            for (let i = 0; i < node.childNodes.length; i++) {
                children += walk(node.childNodes[i]);
            }

            switch (tag) {
                case "p": return children + "\n\n";
                case "h1": return "# " + children + "\n\n";
                case "h2": return "## " + children + "\n\n";
                case "h3": return "### " + children + "\n\n";
                case "h4": return "#### " + children + "\n\n";
                case "h5": return "##### " + children + "\n\n";
                case "h6": return "###### " + children + "\n\n";
                case "strong": case "b": {
                    if (!children.trim()) return "";
                    const match = children.match(/^(\s*)(.*?)(\s*)$/s);
                    return match[1] + "**" + match[2] + "**" + match[3];
                }
                case "em": case "i": {
                    if (!children.trim()) return "";
                    const match = children.match(/^(\s*)(.*?)(\s*)$/s);
                    return match[1] + "*" + match[2] + "*" + match[3];
                }
                case "del": case "s": case "strike": {
                    if (!children.trim()) return "";
                    const match = children.match(/^(\s*)(.*?)(\s*)$/s);
                    return match[1] + "~~" + match[2] + "~~" + match[3];
                }
                case "code": {
                    // Inside <pre> = fenced block handled by "pre" case; standalone = inline
                    if (node.parentNode && node.parentNode.tagName.toLowerCase() === "pre") {
                        return children; // content only, <pre> wraps it
                    }
                    if (!children.trim()) return "";
                    return "`" + children + "`";
                }
                case "pre": {
                    const inner = children.trim();
                    return "\n\n```\n" + inner + "\n```\n\n";
                }
                case "blockquote": {
                    // Prefix each line with "> "
                    return children.split('\n').map(l => l.trim() ? '> ' + l : l).join('\n') + '\n\n';
                }
                case "a": {
                    const href = node.getAttribute("href") || "";
                    const label = children.trim() || href;
                    return `[${label}](${href})`;
                }
                case "li": {
                    const parent = node.parentNode.tagName.toLowerCase();
                    if (parent === "ol") {
                        const index = Array.from(node.parentNode.children).indexOf(node) + 1;
                        return index + ". " + children + "\n";
                    }
                    return "- " + children + "\n";
                }
                case "ul": case "ol": return children + "\n";
                case "br": return "\n";
                case "hr": return "---\n\n";
                case "div": return children + "\n";
                case "img": {
                    const alt = node.getAttribute("alt") || "";
                    const src = node.getAttribute("src") || "";
                    return `![${alt}](${src})`;
                }
                default: return children;
            }
        }

        let result = "";
        for (let i = 0; i < el.childNodes.length; i++) {
            result += walk(el.childNodes[i]);
        }
        
        return result.replace(/\n{3,}/g, '\n\n').trim();
    }
};
