// Makes every Bootstrap modal in the app resizable by a grip in its bottom-right corner, like a
// desktop window. Applied globally via event delegation on 'shown.bs.modal' — no per-page wiring
// needed, so it covers every existing and future modal without touching their markup. Works
// alongside modal-drag-manager.js: drag moves the dialog (transform), resize sets its width/height,
// and the two are independent.
const ModalResizeManager = (() => {
    // The grip + listeners are attached once per modal element and reused across repeated
    // show/hide cycles, rather than re-attached on every open.
    const wired = new WeakSet();

    // Smallest the dialog may be shrunk to, and how much viewport must remain around it.
    const MIN_WIDTH = 320;
    const MIN_HEIGHT = 200;
    const VIEWPORT_MARGIN = 20;

    const makeResizable = (modalEl) => {
        if (wired.has(modalEl)) return;
        wired.add(modalEl);

        const dialog = modalEl.querySelector('.modal-dialog');
        const content = modalEl.querySelector('.modal-content');
        if (!dialog || !content) return;

        // Scopes the resize-only CSS (grip position, body scroll) to modals this manager touches.
        content.classList.add('modal-resizable');

        const handle = document.createElement('div');
        handle.className = 'modal-resize-handle';
        handle.setAttribute('aria-hidden', 'true');
        content.appendChild(handle);

        let resizing = false;
        let pointerId = null;
        let startClientX = 0;
        let startClientY = 0;
        let startWidth = 0;
        let startHeight = 0;

        const onPointerMove = (e) => {
            if (!resizing || e.pointerId !== pointerId) return;

            const maxWidth = window.innerWidth - VIEWPORT_MARGIN;
            const maxHeight = window.innerHeight - VIEWPORT_MARGIN;

            const newWidth = Math.min(Math.max(startWidth + (e.clientX - startClientX), MIN_WIDTH), maxWidth);
            const newHeight = Math.min(Math.max(startHeight + (e.clientY - startClientY), MIN_HEIGHT), maxHeight);

            // width on the dialog (overriding the .modal-lg/.modal-dialog max-width cap), height on
            // the content; its flex column lets the body take the slack and scroll.
            dialog.style.width = `${newWidth}px`;
            dialog.style.maxWidth = `${newWidth}px`;
            content.style.height = `${newHeight}px`;
        };

        const stopResizing = () => {
            resizing = false;
            pointerId = null;
            content.classList.remove('modal-content-resizing');
            document.removeEventListener('pointermove', onPointerMove);
            document.removeEventListener('pointerup', stopResizing);
            document.removeEventListener('pointercancel', stopResizing);
        };

        handle.addEventListener('pointerdown', (e) => {
            // Only the primary mouse button / a single touch/pen contact starts a resize.
            if (e.button !== undefined && e.button !== 0) return;

            // Keep the grip from bubbling into a header drag or closing the modal.
            e.preventDefault();
            e.stopPropagation();

            resizing = true;
            pointerId = e.pointerId;
            startClientX = e.clientX;
            startClientY = e.clientY;

            const dialogRect = dialog.getBoundingClientRect();
            const contentRect = content.getBoundingClientRect();
            startWidth = dialogRect.width;
            startHeight = contentRect.height;

            content.classList.add('modal-content-resizing');
            document.addEventListener('pointermove', onPointerMove);
            document.addEventListener('pointerup', stopResizing);
            document.addEventListener('pointercancel', stopResizing);
        });

        modalEl.addEventListener('hidden.bs.modal', () => {
            // Reopen at the default size next time rather than whatever it was last stretched to.
            dialog.style.width = '';
            dialog.style.maxWidth = '';
            content.style.height = '';
        });
    };

    document.addEventListener('shown.bs.modal', (e) => makeResizable(e.target));

    return { makeResizable };
})();
