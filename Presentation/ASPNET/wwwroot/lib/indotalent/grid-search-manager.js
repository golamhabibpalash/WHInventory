const GridSearchManager = (() => {
    const timers = new WeakMap();

    function wire(inputEl) {
        const gridEl = inputEl.closest('.e-grid');
        if (!gridEl) return;

        const instances = gridEl.ej2_instances;
        if (!instances || !instances.length) return;

        const grid = instances.find(inst => inst && typeof inst.search === 'function');
        if (!grid) return;

        inputEl.addEventListener('input', function () {
            clearTimeout(timers.get(this));
            const el = this;
            timers.set(el, setTimeout(() => grid.search(el.value), 200));
        });
    }

    function attach(inputEl) {
        if (inputEl.dataset.liveSearch) return;
        inputEl.dataset.liveSearch = '1';
        // Defer so Syncfusion finishes addInstance() before we read ej2_instances
        setTimeout(() => wire(inputEl), 100);
    }

    function scan(root) {
        root.querySelectorAll('.e-search-wrapper input.e-input').forEach(attach);
    }

    function init() {
        scan(document.body);
        new MutationObserver(mutations => {
            for (const { addedNodes } of mutations) {
                for (const node of addedNodes) {
                    if (node.nodeType !== 1) continue;
                    if (node.matches('.e-search-wrapper input.e-input')) attach(node);
                    else scan(node);
                }
            }
        }).observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
