/**
 * Robust copy to clipboard supporting modern API and execCommand fallback.
 * Works across all browsers, secure and non-secure HTTP contexts.
 */
export async function copyTextToClipboard(text) {
  if (!text) return false;

  // 1. Try navigator.clipboard.writeText if available
  if (navigator?.clipboard && typeof navigator.clipboard.writeText === 'function') {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch (e) {
      console.warn('navigator.clipboard.writeText failed, attempting fallback', e);
    }
  }

  // 2. Fallback: temporary textarea + document.execCommand('copy')
  try {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.top = '0';
    textArea.style.left = '0';
    textArea.style.width = '2em';
    textArea.style.height = '2em';
    textArea.style.padding = '0';
    textArea.style.border = 'none';
    textArea.style.outline = 'none';
    textArea.style.boxShadow = 'none';
    textArea.style.background = 'transparent';
    textArea.style.opacity = '0';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    const successful = document.execCommand('copy');
    document.body.removeChild(textArea);
    if (successful) return true;
  } catch (err) {
    console.error('execCommand copy fallback failed', err);
  }

  return false;
}

/**
 * Robust paste from clipboard supporting modern API with fallback prompt.
 */
export async function pasteTextFromClipboard() {
  if (navigator?.clipboard && typeof navigator.clipboard.readText === 'function') {
    try {
      const text = await navigator.clipboard.readText();
      if (text && text.trim()) return text.trim();
    } catch (e) {
      console.warn('navigator.clipboard.readText failed or was blocked by browser', e);
    }
  }

  // Fallback if browser blocks clipboard read
  try {
    const input = window.prompt('Paste your Item ID here:');
    if (input && input.trim()) return input.trim();
  } catch {}

  return '';
}
