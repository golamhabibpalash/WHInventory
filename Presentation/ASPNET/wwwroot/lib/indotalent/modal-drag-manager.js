// Makes every Bootstrap modal in the app draggable by its header, like a desktop window.
// Applied globally via event delegation on 'shown.bs.modal' / 'hidden.bs.modal' — no per-page
// wiring needed, so it covers every existing and future modal without touching their markup.
const ModalDragManager = (() => {
    // Header pointerdown/move/up listeners are attached once per modal element and reused
    // across repeated show/hide cycles, rather than re-attached on every open.
    const wired = new WeakSet();

    // Leaves at least this many px of the dialog inside the viewport on every edge, so a
    // modal dragged toward an edge can always be grabbed again.
    const EDGE_MARGIN = 40;

    const makeDraggable = (modalEl) => {
        if (wired.has(modalEl)) return;
        wired.add(modalEl);

        const header = modalEl.querySelector('.modal-header');
        const dialog = modalEl.querySelector('.modal-dialog');
        if (!header || !dialog) return;

        header.classList.add('modal-header-draggable');

        let dragging = false;
        let pointerId = null;
        let startClientX = 0;
        let startClientY = 0;
        let startTranslateX = 0;
        let startTranslateY = 0;
        let startRect = null;

        const currentTranslate = () => {
            const matrix = new DOMMatrixReadOnly(getComputedStyle(dialog).transform);
            return { x: matrix.m41, y: matrix.m42 };
        };

        const onPointerMove = (e) => {
            if (!dragging || e.pointerId !== pointerId) return;

            const dx = e.clientX - startClientX;
            const dy = e.clientY - startClientY;

            const minDx = EDGE_MARGIN - startRect.right;
            const maxDx = window.innerWidth - EDGE_MARGIN - startRect.left;
            const minDy = EDGE_MARGIN - startRect.bottom;
            const maxDy = window.innerHeight - EDGE_MARGIN - startRect.top;

            const clampedDx = Math.min(Math.max(dx, minDx), maxDx);
            const clampedDy = Math.min(Math.max(dy, minDy), maxDy);

            dialog.style.transform = `translate(${startTranslateX + clampedDx}px, ${startTranslateY + clampedDy}px)`;
        };

        const stopDragging = () => {
            dragging = false;
            pointerId = null;
            header.classList.remove('modal-header-dragging');
            document.removeEventListener('pointermove', onPointerMove);
            document.removeEventListener('pointerup', stopDragging);
            document.removeEventListener('pointercancel', stopDragging);
        };

        header.addEventListener('pointerdown', (e) => {
            // Let the close button, or anything interactive placed in a custom header, work
            // normally instead of starting a drag.
            if (e.target.closest('.btn-close, button, a, input, select, textarea')) return;
            // Only the primary mouse button / a single touch/pen contact starts a drag.
            if (e.button !== undefined && e.button !== 0) return;

            dragging = true;
            pointerId = e.pointerId;
            startClientX = e.clientX;
            startClientY = e.clientY;
            startRect = dialog.getBoundingClientRect();
            const translate = currentTranslate();
            startTranslateX = translate.x;
            startTranslateY = translate.y;

            header.classList.add('modal-header-dragging');
            document.addEventListener('pointermove', onPointerMove);
            document.addEventListener('pointerup', stopDragging);
            document.addEventListener('pointercancel', stopDragging);
        });

        modalEl.addEventListener('hidden.bs.modal', () => {
            // Reopen centered next time rather than wherever it was last left.
            dialog.style.transform = '';
        });
    };

    document.addEventListener('shown.bs.modal', (e) => makeDraggable(e.target));

    return { makeDraggable };
})();
