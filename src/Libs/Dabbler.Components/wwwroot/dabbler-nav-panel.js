// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

// JS interop module for DabblerNavigationPanel divider drag behavior.
// Handles pointer capture and smooth resize without per-pixel C# interop.

/**
 * Initializes drag behavior on the divider element.
 * @param {HTMLElement} dividerEl - The divider handle element
 * @param {HTMLElement} containerEl - The parent flex-column container
 * @param {DotNetObjectReference} dotNetRef - .NET callback reference
 * @param {number} initialPercent - Initial split percentage for the top section
 */
export function initDrag(dividerEl, containerEl, dotNetRef, initialPercent) {
    if (!dividerEl || !containerEl) return;

    // Clean up any previous listeners
    if (dividerEl._cleanup) {
        dividerEl._cleanup();
    }

    let startY = 0;
    let startPercent = initialPercent;
    let containerHeight = 0;

    function onPointerDown(e) {
        e.preventDefault();
        startY = e.clientY;
        containerHeight = containerEl.clientHeight;

        const topSection = containerEl.querySelector('.dabbler-nav-entity-list');
        if (topSection) {
            const basis = parseFloat(topSection.style.flexBasis);
            if (!isNaN(basis)) startPercent = basis;
        }

        dividerEl.setPointerCapture(e.pointerId);
        containerEl.classList.add('dabbler-nav-content-dragging');
    }

    function onPointerMove(e) {
        if (!dividerEl.hasPointerCapture(e.pointerId)) return;
        if (containerHeight <= 0) return;

        const deltaPixels = e.clientY - startY;
        const deltaPercent = (deltaPixels / containerHeight) * 100;
        const newPercent = Math.max(15, Math.min(85, startPercent + deltaPercent));

        const topSection = containerEl.querySelector('.dabbler-nav-entity-list');
        if (topSection) {
            topSection.style.flexBasis = newPercent + '%';
        }
    }

    function onPointerUp(e) {
        if (!dividerEl.hasPointerCapture(e.pointerId)) return;
        dividerEl.releasePointerCapture(e.pointerId);
        containerEl.classList.remove('dabbler-nav-content-dragging');

        const topSection = containerEl.querySelector('.dabbler-nav-entity-list');
        const finalPercent = parseFloat(topSection?.style.flexBasis) || startPercent;
        dotNetRef.invokeMethodAsync('OnDividerDragEnd', finalPercent);
    }

    dividerEl.addEventListener('pointerdown', onPointerDown);
    dividerEl.addEventListener('pointermove', onPointerMove);
    dividerEl.addEventListener('pointerup', onPointerUp);

    dividerEl._cleanup = () => {
        dividerEl.removeEventListener('pointerdown', onPointerDown);
        dividerEl.removeEventListener('pointermove', onPointerMove);
        dividerEl.removeEventListener('pointerup', onPointerUp);
        delete dividerEl._cleanup;
    };
}

/**
 * Cleans up drag event listeners from the divider element.
 * @param {HTMLElement} dividerEl
 */
export function dispose(dividerEl) {
    if (dividerEl?._cleanup) {
        dividerEl._cleanup();
    }
}
