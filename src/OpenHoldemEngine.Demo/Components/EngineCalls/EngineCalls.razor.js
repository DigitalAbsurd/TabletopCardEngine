/**
 * Engine calls component JavaScript interop.
 * Handles Prism.js highlighting and auto-scroll behavior.
 */

/**
 * Highlights code blocks using Prism.js.
 * Only highlights blocks that haven't been highlighted yet to avoid re-processing.
 */
export function highlightCode() {
    // Check if Prism is available
    if (typeof Prism === 'undefined') {
        console.warn('Prism.js not loaded');
        return;
    }

    // Find all unprocessed code blocks in the engine calls list
    const codeBlocks = document.querySelectorAll('.engine-calls-list code:not(.hljs)');

    codeBlocks.forEach(block => {
        Prism.highlightElement(block);
    });
}

/**
 * Scrolls to the last engine call entry.
 * @param {HTMLElement} containerElement - The container element to scroll
 */
export function scrollToLatestEntry(containerElement) {
    if (!containerElement) {
        return;
    }

    // Wait for DOM to be fully updated before scrolling
    requestAnimationFrame(() => {
        const entries = containerElement.querySelectorAll('.engine-call-entry');
        if (entries.length > 0) {
            const lastEntry = entries[entries.length - 1];
            lastEntry.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    });
}
