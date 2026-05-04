// Stub DotNet interop and browser APIs not present in jsdom
globalThis.DotNet = {
  invokeMethodAsync: vi.fn().mockResolvedValue(null),
}

// execCommand stub (not supported in jsdom)
document.execCommand = vi.fn().mockReturnValue(true)

// Load the library JS
import '../../src/WysiMd.Blazor/wwwroot/WysiMd.Blazor.js'
