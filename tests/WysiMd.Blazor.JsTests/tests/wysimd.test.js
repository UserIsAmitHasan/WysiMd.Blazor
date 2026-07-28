/**
 * Unit tests for WysiMd.Blazor.js vanilla JS functions.
 * These tests run in jsdom via Vitest.
 */

// -----------------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------------

function makeTextarea(id, value = '') {
  const ta = document.createElement('textarea')
  ta.id = id
  ta.value = value
  document.body.appendChild(ta)
  return ta
}

function cleanup(id) {
  const el = document.getElementById(id)
  if (el) el.remove()
}

// -----------------------------------------------------------------------
// getSelection
// -----------------------------------------------------------------------

describe('WysiMdBlazor.getSelection', () => {
  const ID = 'test-ta-getsel'

  beforeEach(() => makeTextarea(ID, 'hello world'))
  afterEach(() => cleanup(ID))

  it('returns start and end when no selection', () => {
    const result = WysiMdBlazor.getSelection(ID)
    expect(result).toHaveProperty('start')
    expect(result).toHaveProperty('end')
    expect(result).toHaveProperty('value')
  })

  it('returns the textarea value', () => {
    const result = WysiMdBlazor.getSelection(ID)
    expect(result.value).toBe('hello world')
  })

  it('returns null for missing element', () => {
    const result = WysiMdBlazor.getSelection('nonexistent-id')
    expect(result).toBeNull()
  })
})

// -----------------------------------------------------------------------
// setSelection
// -----------------------------------------------------------------------

describe('WysiMdBlazor.setSelection', () => {
  const ID = 'test-ta-setsel'

  beforeEach(() => makeTextarea(ID, 'hello world'))
  afterEach(() => cleanup(ID))

  it('sets selectionStart and selectionEnd', () => {
    WysiMdBlazor.setSelection(ID, 3, 7)
    const ta = document.getElementById(ID)
    expect(ta.selectionStart).toBe(3)
    expect(ta.selectionEnd).toBe(7)
  })

  it('clamps end to value length', () => {
    WysiMdBlazor.setSelection(ID, 0, 999)
    const ta = document.getElementById(ID)
    expect(ta.selectionEnd).toBeLessThanOrEqual(ta.value.length)
  })

  it('does not throw for missing element', () => {
    expect(() => WysiMdBlazor.setSelection('no-id', 0, 0)).not.toThrow()
  })
})

// -----------------------------------------------------------------------
// setValueAndSelection
// -----------------------------------------------------------------------

describe('WysiMdBlazor.setValueAndSelection', () => {
  const ID = 'test-ta-setval'

  beforeEach(() => makeTextarea(ID, ''))
  afterEach(() => cleanup(ID))

  it('updates textarea value', () => {
    WysiMdBlazor.setValueAndSelection(ID, 'new content', 0, 0)
    const ta = document.getElementById(ID)
    expect(ta.value).toBe('new content')
  })

  it('sets selection after value update', () => {
    WysiMdBlazor.setValueAndSelection(ID, 'abcde', 2, 4)
    const ta = document.getElementById(ID)
    expect(ta.selectionStart).toBe(2)
    expect(ta.selectionEnd).toBe(4)
  })

  it('dispatches input event', () => {
    const ta = document.getElementById(ID)
    const spy = vi.fn()
    ta.addEventListener('input', spy)

    WysiMdBlazor.setValueAndSelection(ID, 'triggered', 0, 0)
    expect(spy).toHaveBeenCalled()
  })

  it('does not throw for missing element', () => {
    expect(() => WysiMdBlazor.setValueAndSelection('no-id', 'val', 0, 0)).not.toThrow()
  })
})

// -----------------------------------------------------------------------
// downloadFile
// -----------------------------------------------------------------------

describe('WysiMdBlazor.downloadFile', () => {
  it('creates and clicks a download link', () => {
    const appendSpy = vi.spyOn(document.body, 'appendChild').mockImplementation(() => {})
    const removeSpy = vi.spyOn(document.body, 'removeChild').mockImplementation(() => {})

    // Create a fake anchor with a click spy
    const fakeAnchor = { href: '', download: '', click: vi.fn(), style: {} }
    vi.spyOn(document, 'createElement').mockReturnValueOnce(fakeAnchor)

    WysiMdBlazor.downloadFile('test.md', 'data:text/plain;base64,aGVsbG8=')

    expect(fakeAnchor.download).toBe('test.md')
    expect(fakeAnchor.click).toHaveBeenCalled()

    appendSpy.mockRestore()
    removeSpy.mockRestore()
    vi.restoreAllMocks()
  })
})

// -----------------------------------------------------------------------
// clickElement
// -----------------------------------------------------------------------

describe('WysiMdBlazor.clickElement', () => {
  const ID = 'test-click-btn'

  beforeEach(() => {
    const btn = document.createElement('button')
    btn.id = ID
    document.body.appendChild(btn)
  })
  afterEach(() => cleanup(ID))

  it('calls click on the element', () => {
    const el = document.getElementById(ID)
    const spy = vi.spyOn(el, 'click')
    WysiMdBlazor.clickElement(ID)
    expect(spy).toHaveBeenCalled()
  })

  it('does not throw for missing element', () => {
    expect(() => WysiMdBlazor.clickElement('nonexistent')).not.toThrow()
  })
})

// -----------------------------------------------------------------------
// saveSelection / restoreSelection
// -----------------------------------------------------------------------

describe('WysiMdBlazor.saveSelection / restoreSelection', () => {
  it('does not throw when no selection is active', () => {
    expect(() => WysiMdBlazor.saveSelection()).not.toThrow()
    expect(() => WysiMdBlazor.restoreSelection()).not.toThrow()
  })
})

// -----------------------------------------------------------------------
// execCommand
// -----------------------------------------------------------------------

describe('WysiMdBlazor.execCommand', () => {
  it('calls document.execCommand with provided args', () => {
    document.execCommand.mockClear()
    WysiMdBlazor.execCommand('bold', null)
    expect(document.execCommand).toHaveBeenCalledWith('bold', false, null)
  })

  it('does not throw on unknown command', () => {
    expect(() => WysiMdBlazor.execCommand('unknown-command', null)).not.toThrow()
  })
})

// -----------------------------------------------------------------------
// Namespace guard
// -----------------------------------------------------------------------

describe('WysiMdBlazor namespace', () => {
  it('is exposed on window', () => {
    expect(window.WysiMdBlazor).toBeDefined()
  })

  it('exposes expected public functions', () => {
    const expected = [
      'getSelection',
      'setSelection',
      'setValueAndSelection',
      'downloadFile',
      'clickElement',
      'saveSelection',
      'restoreSelection',
      'execCommand',
      'registerShortcuts',
      'unregisterSelectionListener',
      'setPreviewHtml',
      'getCaretOffset',
      'setCaretOffset',
      'insertHtmlAtSelection',
    ]
    for (const fn of expected) {
      expect(typeof WysiMdBlazor[fn]).toBe('function', `Expected WysiMdBlazor.${fn} to be a function`)
    }
  })
})
