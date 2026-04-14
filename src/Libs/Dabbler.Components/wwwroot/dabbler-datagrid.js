// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

// JS interop module for DabblerDataGrid keyboard navigation.
// Manages visual focus highlight and scroll-into-view for grid cells.

const FOCUS_CLASS = "dabbler-grid-cell-focus";

/**
 * Returns navigable rows (data rows and group headers, excluding detail content rows).
 * @param {HTMLElement} tbody
 * @returns {HTMLElement[]}
 */
function getNavigableRows(tbody) {
    return Array.from(tbody.querySelectorAll(":scope > tr"))
        .filter(tr => !tr.classList.contains("dabbler-detail-row"));
}

/**
 * Highlights the cell at (rowIndex, colIndex) within the grid container,
 * removing the previous highlight. Scrolls the cell into view.
 * Detail content rows are excluded from the row index — only data rows and
 * group header rows are counted.
 * @param {HTMLElement} container - The wrapper element containing the MudDataGrid.
 * @param {number} rowIndex - Zero-based row index within the visible tbody rows (excludes detail rows).
 * @param {number} colIndex - Zero-based column index within the row.
 */
export function focusCell(container, rowIndex, colIndex) {
    if (!container) return;

    // Remove previous focus highlight and ARIA attributes
    const prev = container.querySelector("." + FOCUS_CLASS);
    if (prev) {
        prev.classList.remove(FOCUS_CLASS);
        prev.removeAttribute("aria-current");
        prev.removeAttribute("id");
    }

    // Find the target cell
    const tbody = container.querySelector("tbody.mud-table-body");
    if (!tbody) return;

    // Exclude dabbler-detail-row elements — they are not part of the navigation model
    const rows = getNavigableRows(tbody);
    if (rowIndex < 0 || rowIndex >= rows.length) return;

    // Set aria-rowindex on the row (1-based per ARIA spec)
    rows[rowIndex].setAttribute("aria-rowindex", rowIndex + 1);

    const cells = rows[rowIndex].querySelectorAll(":scope > td");
    if (colIndex < 0 || colIndex >= cells.length) return;

    const cell = cells[colIndex];
    cell.classList.add(FOCUS_CLASS);

    // ARIA: mark the active cell for aria-activedescendant
    const cellId = "dabbler-grid-cell-" + rowIndex + "-" + colIndex;
    cell.setAttribute("id", cellId);
    cell.setAttribute("aria-current", "true");
    cell.setAttribute("role", "gridcell");
    cell.setAttribute("aria-colindex", colIndex + 1);

    cell.scrollIntoView({ block: "nearest", inline: "nearest" });

    // Return the cell id so Blazor can set aria-activedescendant
    return cellId;
}

/**
 * Removes all focus highlights from the grid container.
 * @param {HTMLElement} container - The wrapper element.
 */
export function clearFocus(container) {
    if (!container) return;
    const prev = container.querySelector("." + FOCUS_CLASS);
    if (prev) {
        prev.classList.remove(FOCUS_CLASS);
        prev.removeAttribute("aria-current");
        prev.removeAttribute("id");
    }
}

/**
 * Returns the number of navigable rows in the tbody (excludes detail content rows).
 * @param {HTMLElement} container - The wrapper element.
 * @returns {number} The count of navigable rows.
 */
export function getVisibleRowCount(container) {
    if (!container) return 0;
    const tbody = container.querySelector("tbody.mud-table-body");
    if (!tbody) return 0;
    return getNavigableRows(tbody).length;
}

/**
 * Estimates the number of rows visible in the viewport (for Page Up/Down).
 * Detail content rows are excluded from the height calculation.
 * @param {HTMLElement} container - The wrapper element.
 * @returns {number} Approximate rows per viewport page.
 */
export function getViewportRowCount(container) {
    if (!container) return 10;
    const tbody = container.querySelector("tbody.mud-table-body");
    if (!tbody) return 10;
    const rows = getNavigableRows(tbody);
    if (rows.length === 0) return 10;

    const rowHeight = rows[0].getBoundingClientRect().height;
    if (rowHeight === 0) return 10;

    // Use the table container's visible height
    const tableContainer = container.querySelector(".mud-table-container");
    const viewportHeight = tableContainer
        ? tableContainer.clientHeight
        : container.clientHeight;

    return Math.max(1, Math.floor(viewportHeight / rowHeight));
}

/**
 * Focuses the first element with tabindex="0" inside the detail region.
 * Falls back to focusing the region element itself if no focusable child is found.
 * Uses a short delay so Blazor has time to render the detail content.
 * @param {string} detailId - The id of the detail region element.
 */
export function focusDetailContent(detailId) {
    // Allow Blazor to render the newly-expanded detail row before focusing
    setTimeout(() => {
        const region = document.getElementById(detailId);
        if (!region) return;
        const focusable = region.querySelector("[tabindex='0']");
        if (focusable) {
            focusable.focus();
        } else {
            // Fallback: focus the region itself so keyboard events reach the detail area
            region.setAttribute("tabindex", "-1");
            region.focus();
        }
    }, 80);
}

/**
 * Focuses the given element directly (used to return focus to a parent grid).
 * @param {HTMLElement} element - The element to focus.
 */
export function focusElement(element) {
    if (element) element.focus();
}

// ── Keyboard interceptor ───────────────────────────────────────────
// Registers a capture-phase keydown handler that calls preventDefault()
// synchronously for navigation keys. This avoids the Blazor render-cycle
// lag of @onkeydown:preventDefault which evaluates at render time.

const _interceptors = new WeakMap();

const _NAV_KEYS = new Set([
    'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
    'Enter', 'F2', 'Home', 'End', 'PageUp', 'PageDown'
]);

/**
 * Registers a capture-phase keydown handler on the container that
 * calls preventDefault() for navigation keys.
 * - Tab and Escape are always prevented (grid handles navigation/cancel).
 * - Arrow keys, Enter, F2, Home, End, PageUp, PageDown are prevented
 *   only when the active element is NOT an input/textarea/select
 *   (so text editing inside cell editors still works).
 * @param {HTMLElement} container - The wrapper element.
 */
export function registerKeyInterceptor(container) {
    if (!container || _interceptors.has(container)) return;

    const handler = (e) => {
        const active = document.activeElement;
        const isInput = active &&
            (active.tagName === 'INPUT' ||
             active.tagName === 'TEXTAREA' ||
             active.tagName === 'SELECT' ||
             active.isContentEditable);

        if (e.key === 'Tab' || e.key === 'Escape') {
            e.preventDefault();
        } else if (!isInput && (_NAV_KEYS.has(e.key) || e.key === ' ')) {
            e.preventDefault();
        }
    };

    container.addEventListener('keydown', handler, true);
    _interceptors.set(container, handler);
}

/**
 * Removes the capture-phase keydown handler registered by registerKeyInterceptor.
 * @param {HTMLElement} container - The wrapper element.
 */
export function unregisterKeyInterceptor(container) {
    if (!container) return;
    const handler = _interceptors.get(container);
    if (handler) {
        container.removeEventListener('keydown', handler, true);
        _interceptors.delete(container);
    }
}

/**
 * Returns true if the document's active element is inside the container.
 * Used to debounce focusout — if focus moved to another child element
 * within the grid, we should not clear the focus visual.
 * @param {HTMLElement} container - The wrapper element.
 * @returns {boolean}
 */
export function isFocusWithin(container) {
    if (!container) return false;
    return container.contains(document.activeElement);
}

/**
 * Returns true if the active element inside the container is an input,
 * textarea, select, or contenteditable element (i.e., the user is editing).
 * @param {HTMLElement} container - The wrapper element.
 * @returns {boolean}
 */
export function isInputFocused(container) {
    const active = document.activeElement;
    if (!active || !container || !container.contains(active)) return false;
    return active.tagName === 'INPUT' ||
           active.tagName === 'TEXTAREA' ||
           active.tagName === 'SELECT' ||
           active.isContentEditable;
}

/**
 * Focuses the first input/textarea/select inside the cell at (rowIndex, colIndex).
 * Uses a short delay so the edit template has time to render.
 * @param {HTMLElement} container - The wrapper element.
 * @param {number} rowIndex - Zero-based row index.
 * @param {number} colIndex - Zero-based column index.
 */
export function focusEditorInCell(container, rowIndex, colIndex) {
    setTimeout(() => {
        if (!container) return;
        const tbody = container.querySelector("tbody.mud-table-body") ||
                      container.querySelector("tbody");
        if (!tbody) return;

        const rows = Array.from(tbody.querySelectorAll(":scope > tr"))
            .filter(tr => !tr.classList.contains("dabbler-detail-row") &&
                          !tr.classList.contains("mud-table-empty-row"));
        if (rowIndex < 0 || rowIndex >= rows.length) return;

        const cells = Array.from(rows[rowIndex].querySelectorAll(":scope > td"));
        if (colIndex < 0 || colIndex >= cells.length) return;

        const input = cells[colIndex].querySelector(
            "input, textarea, select, [contenteditable='true']");
        if (input) {
            input.focus();
            if (typeof input.select === 'function') input.select();
        }
    }, 80);
}

/**
 * Applies sticky column pinning to grid cells after render.
 *
 * Measures column widths from the first header row (or first data row if no
 * header is present), then sets position:sticky with computed left/right offsets
 * on all matching cells. Group-header rows and detail rows are excluded — their
 * full-width <td> cells keep position:static via CSS.
 *
 * @param {HTMLElement} container - The wrapper element.
 * @param {Array<{columnIndex: number, position: 'left'|'right'}>} pinConfig
 *   - columnIndex: 0-based DOM cell index within each row (accounting for any indicator column)
 *   - position: 'left' or 'right'
 */
export function applyPinOffsets(container, pinConfig) {
    if (!container || !pinConfig || pinConfig.length === 0) return;

    const table = container.querySelector("table");
    if (!table) return;

    const leftPins  = pinConfig.filter(c => c.position === 'left').map(c => c.columnIndex);
    const rightPins = pinConfig.filter(c => c.position === 'right').map(c => c.columnIndex);
    if (leftPins.length === 0 && rightPins.length === 0) return;

    const lastLeft   = leftPins.length  > 0 ? Math.max(...leftPins)  : -1;
    const firstRight = rightPins.length > 0 ? Math.min(...rightPins) : Infinity;

    // Determine column widths from the header row or first navigable data row
    const measureRow =
        table.querySelector("thead tr") ||
        getNavigableRows(table.querySelector("tbody.mud-table-body"))?.at(0);

    if (!measureRow) return;

    const measureCells = Array.from(measureRow.querySelectorAll(":scope > th, :scope > td"));
    const colWidths = measureCells.map(cell => cell.getBoundingClientRect().width);
    if (colWidths.length === 0) return;

    // Compute cumulative offsets from the left
    const leftOffsets = [];
    let cumLeft = 0;
    for (let i = 0; i < colWidths.length; i++) {
        leftOffsets.push(cumLeft);
        cumLeft += colWidths[i];
    }

    // Compute cumulative offsets from the right
    const rightOffsets = new Array(colWidths.length).fill(0);
    let cumRight = 0;
    for (let i = colWidths.length - 1; i >= 0; i--) {
        rightOffsets[i] = cumRight;
        cumRight += colWidths[i];
    }

    // Apply pin classes and offsets to every non-group, non-detail row
    const PIN_CLASSES = [
        'dabbler-col-pin-left',
        'dabbler-col-pin-right',
        'dabbler-col-pin-left-last',
        'dabbler-col-pin-right-first'
    ];

    const allRows = Array.from(table.querySelectorAll("tr"));
    for (const row of allRows) {
        // Group header and detail rows: full-width td, leave them to CSS (position:static !important)
        if (row.classList.contains('dabbler-group-header') ||
            row.classList.contains('dabbler-detail-row')) {
            continue;
        }

        const cells = Array.from(row.querySelectorAll(":scope > th, :scope > td"));
        for (let i = 0; i < cells.length; i++) {
            const cell = cells[i];

            // Remove previous pin state
            cell.classList.remove(...PIN_CLASSES);
            cell.style.left  = '';
            cell.style.right = '';

            if (leftPins.includes(i)) {
                cell.classList.add('dabbler-col-pin-left');
                if (i < leftOffsets.length) cell.style.left = leftOffsets[i] + 'px';
                if (i === lastLeft) cell.classList.add('dabbler-col-pin-left-last');
            } else if (rightPins.includes(i)) {
                cell.classList.add('dabbler-col-pin-right');
                if (i < rightOffsets.length) cell.style.right = rightOffsets[i] + 'px';
                if (i === firstRight) cell.classList.add('dabbler-col-pin-right-first');
            }
        }
    }
}
