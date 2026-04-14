// Accessibility helpers for DabblerComboBox / DabblerMultiComboBox.
// Labels MudBlazor's internally-rendered clear buttons and other icon-only
// buttons that don't expose aria-label parameters.

/**
 * Finds all icon-only buttons (MudIconButton) within the given element that
 * lack an aria-label and labels them based on their icon SVG content.
 * @param {HTMLElement} root - The combobox wrapper element.
 */
export function labelClearButtons(root) {
    if (!root) return;
    const buttons = root.querySelectorAll('button.mud-button-root.mud-icon-button');
    for (const btn of buttons) {
        if (btn.getAttribute('aria-label')) continue;
        // MudBlazor's clear button uses the Clear icon SVG path
        const svg = btn.querySelector('svg');
        if (svg) {
            const path = svg.querySelector('path');
            if (path) {
                const d = path.getAttribute('d') || '';
                // Clear/close icon path starts with "M19 6.41"
                if (d.startsWith('M19 6.41') || d.startsWith('M12 2C6')) {
                    btn.setAttribute('aria-label', 'Clear selection');
                }
            }
        }
    }
}
