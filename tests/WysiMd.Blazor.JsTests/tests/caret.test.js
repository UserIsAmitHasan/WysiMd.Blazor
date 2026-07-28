/**
 * Unit tests for the caret-offset helpers (PR #1):
 * getCaretOffset / setCaretOffset / saveSelection / restoreSelection /
 * setPreviewHtml and the _nodeAtOffset / _offsetInEditor internals.
 * These tests run in jsdom via Vitest.
 */

// -----------------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------------

function makeEditor(id, html = '') {
  const div = document.createElement('div')
  div.id = id
  div.contentEditable = 'true'
  div.innerHTML = html
  document.body.appendChild(div)
  return div
}

function cleanup(id) {
  const el = document.getElementById(id)
  if (el) el.remove()
  window.getSelection()?.removeAllRanges()
  WysiMdBlazor._savedSelection = null
  WysiMdBlazor._lastLiveSelection = null
}

// -----------------------------------------------------------------------
// _nodeAtOffset
// -----------------------------------------------------------------------

describe('WysiMdBlazor._nodeAtOffset', () => {
  const ID = 'test-ce-nodeat'
  afterEach(() => cleanup(ID))

  it('resolves an offset within the first text node', () => {
    const el = makeEditor(ID, '<p>hello</p>')
    const pos = WysiMdBlazor._nodeAtOffset(el, 3)
    expect(pos.node.textContent).toBe('hello')
    expect(pos.offset).toBe(3)
  })

  it('walks across elements to later text nodes', () => {
    const el = makeEditor(ID, '<p>abc</p><p>defg</p>')
    const pos = WysiMdBlazor._nodeAtOffset(el, 5) // "abc" + 2 into "defg"
    expect(pos.node.textContent).toBe('defg')
    expect(pos.offset).toBe(2)
  })

  it('clamps an offset past the end to the last text node', () => {
    const el = makeEditor(ID, '<p>abc</p>')
    const pos = WysiMdBlazor._nodeAtOffset(el, 999)
    expect(pos.node.textContent).toBe('abc')
    expect(pos.offset).toBe(3)
  })

  it('returns the element itself when the editor is empty', () => {
    const el = makeEditor(ID, '')
    const pos = WysiMdBlazor._nodeAtOffset(el, 5)
    expect(pos.node).toBe(el)
    expect(pos.offset).toBe(0)
  })
})

// -----------------------------------------------------------------------
// _offsetInEditor
// -----------------------------------------------------------------------

describe('WysiMdBlazor._offsetInEditor', () => {
  const ID = 'test-ce-offin'
  afterEach(() => cleanup(ID))

  it('is the inverse of _nodeAtOffset across nested elements', () => {
    const el = makeEditor(ID, '<p>abc</p><p><strong>de</strong>fg</p>')
    for (const target of [0, 2, 4, 6]) {
      const pos = WysiMdBlazor._nodeAtOffset(el, target)
      expect(WysiMdBlazor._offsetInEditor(el, pos.node, pos.offset)).toBe(target)
    }
  })
})

// -----------------------------------------------------------------------
// setCaretOffset / getCaretOffset
// -----------------------------------------------------------------------

describe('WysiMdBlazor.setCaretOffset / getCaretOffset', () => {
  const ID = 'test-ce-caret'
  afterEach(() => cleanup(ID))

  it('round-trips a collapsed caret', () => {
    makeEditor(ID, '<p>hello world</p>')
    expect(WysiMdBlazor.setCaretOffset(ID, 4, 4)).toBe(true)
    expect(WysiMdBlazor.getCaretOffset(ID)).toEqual({ start: 4, end: 4 })
  })

  it('round-trips a range selection spanning elements', () => {
    makeEditor(ID, '<p>abc</p><p>defg</p>')
    expect(WysiMdBlazor.setCaretOffset(ID, 1, 5)).toBe(true)
    expect(WysiMdBlazor.getCaretOffset(ID)).toEqual({ start: 1, end: 5 })
  })

  it('clamps offsets beyond the content length', () => {
    makeEditor(ID, '<p>abc</p>')
    expect(WysiMdBlazor.setCaretOffset(ID, 999, 999)).toBe(true)
    const result = WysiMdBlazor.getCaretOffset(ID)
    expect(result.start).toBe(3)
    expect(result.end).toBe(3)
  })

  it('returns false / null for a missing element', () => {
    expect(WysiMdBlazor.setCaretOffset('nonexistent', 0, 0)).toBe(false)
    expect(WysiMdBlazor.getCaretOffset('nonexistent')).toBeNull()
  })
})

// -----------------------------------------------------------------------
// saveSelection / restoreSelection (visual mode, offset based)
// -----------------------------------------------------------------------

describe('WysiMdBlazor.saveSelection / restoreSelection with elementId', () => {
  const ID = 'test-ce-saveres'
  afterEach(() => cleanup(ID))

  it('saves offsets and restores them after the selection is cleared', () => {
    makeEditor(ID, '<p>hello world</p>')
    WysiMdBlazor.setCaretOffset(ID, 6, 11)
    WysiMdBlazor.saveSelection(ID)
    window.getSelection().removeAllRanges()

    expect(WysiMdBlazor.restoreSelection(ID)).toBe(true)
    expect(WysiMdBlazor.getCaretOffset(ID)).toEqual({ start: 6, end: 11 })
  })

  it('falls back to the last live selection when nothing was saved', () => {
    makeEditor(ID, '<p>hello</p>')
    WysiMdBlazor.setCaretOffset(ID, 2, 2)
    window.getSelection().removeAllRanges()

    expect(WysiMdBlazor.restoreSelection(ID)).toBe(true)
    expect(WysiMdBlazor.getCaretOffset(ID)).toEqual({ start: 2, end: 2 })
  })
})

// -----------------------------------------------------------------------
// setPreviewHtml
// -----------------------------------------------------------------------

describe('WysiMdBlazor.setPreviewHtml', () => {
  const ID = 'test-ce-setprev'
  afterEach(() => cleanup(ID))

  it('replaces the element HTML', () => {
    const el = makeEditor(ID, '<p>old</p>')
    WysiMdBlazor.setPreviewHtml(ID, '<h1>new</h1>')
    expect(el.innerHTML).toBe('<h1>new</h1>')
  })

  it('is a no-op when the HTML is unchanged', () => {
    makeEditor(ID, '<p>same</p>')
    WysiMdBlazor.setCaretOffset(ID, 2, 2)
    WysiMdBlazor.setPreviewHtml(ID, '<p>same</p>')
    // Caret tracking survives because the DOM was not touched
    expect(WysiMdBlazor._lastLiveSelection).toEqual({ elementId: ID, start: 2, end: 2 })
  })

  it('resets tracked selection offsets when the DOM is replaced', () => {
    makeEditor(ID, '<p>old</p>')
    WysiMdBlazor.setCaretOffset(ID, 2, 2)
    WysiMdBlazor.setPreviewHtml(ID, '<p>brand new</p>')
    expect(WysiMdBlazor._lastLiveSelection).toEqual({ elementId: ID, start: 0, end: 0 })
  })

  it('does not throw for a missing element', () => {
    expect(() => WysiMdBlazor.setPreviewHtml('nonexistent', '<p>x</p>')).not.toThrow()
  })
})
