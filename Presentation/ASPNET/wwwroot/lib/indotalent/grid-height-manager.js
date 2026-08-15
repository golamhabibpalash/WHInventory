const GridHeightManager = (() => {
    const bottomMargin = 20;
    const minHeight = 150;
    const resizeTimers = new WeakMap();
    const modalHandlers = new WeakMap();

    function computeHeight(element) {
        const rect = element.getBoundingClientRect();

        // A fully hidden element (e.g. a grid inside a closed modal) reports top=0 and height=0.
        // Fall back to a viewport fraction until the modal opens and triggers a re-measure.
        if (rect.top === 0 && rect.height === 0) {
            return Math.floor(window.innerHeight * 0.6);
        }

        return Math.max(minHeight, Math.floor(window.innerHeight - rect.top - bottomMargin));
    }

    function applyHeight(gridObj, element) {
        const height = computeHeight(element);
        if (gridObj && typeof gridObj.setProperties === 'function') {
            gridObj.setProperties({ height: height + 'px' });
        }
    }

    function bindResize(gridObj, element) {
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimers.get(element));
            resizeTimers.set(element, setTimeout(() => applyHeight(gridObj, element), 150));
        });
    }

    function bindModal(gridObj, element) {
        const modal = element.closest('.modal');
        if (!modal) return;

        const update = () => applyHeight(gridObj, element);

        if (modalHandlers.has(modal)) return;
        modalHandlers.set(modal, true);

        modal.addEventListener('shown.bs.modal', update);
        modal.addEventListener('resize', update);
    }

    function apply(gridObj, element) {
        if (!element) return;
        applyHeight(gridObj, element);
        bindResize(gridObj, element);
        bindModal(gridObj, element);
    }

    return { apply };
})();
