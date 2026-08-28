/**
 * Engine calls component JavaScript interop.
 * Handles Prism.js highlighting and auto-scroll behavior.
 */

/**
 * Highlights code blocks using Prism.js.
 * Only highlights blocks that haven't been highlighted yet to avoid re-processing.
 * Uses a data attribute to track which blocks have been highlighted.
 */
export function highlightCode() {
    // Check if Prism is available
    if (typeof Prism === 'undefined') {
        console.warn('Prism.js not loaded');
        return;
    }

    // Find all code blocks that haven't been highlighted yet
    // Use our own data attribute since Prism doesn't set .hljs
    const codeBlocks = document.querySelectorAll('.engine-calls-list code:not([data-highlighted])');

    codeBlocks.forEach(block => {
        Prism.highlightElement(block);
        // Mark this block as highlighted so we don't re-process it
        block.setAttribute('data-highlighted', 'true');
    });
}

/**
 * Scrolls the engine-calls list to show the latest entry.
 * Scrolls only the list element itself, not its ancestors.
 * @param {HTMLElement} listElement - The .engine-calls-list element to scroll
 */
export function scrollToLatestEntry(listElement) {
    if (!listElement) {
        return;
    }

    // Wait for DOM to be fully updated before scrolling
    requestAnimationFrame(() => {
        // Scroll the list element to its bottom (newest entry)
        // scrollHeight is the total height, setting scrollTop to scrollHeight scrolls to the bottom
        listElement.scrollTop = listElement.scrollHeight;
    });
}
