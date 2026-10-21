/*
 * GridActionManager — shared grid interactions used across the list pages.
 *
 * enableRowDoubleClickEdit(gridObj, editItemId?):
 *   Opens a row's Edit action on double-click, reusing the page's existing custom Edit
 *   toolbar item (default id "EditCustom"). Pages opt in with one line right after the grid
 *   is created, e.g.:
 *       GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
 *       GridActionManager.enableRowDoubleClickEdit(mainGrid.obj);
 *
 *   The list grids use a single-select "toggle" (rowSelecting clears the selection when a
 *   row is clicked again), so the second click of a double-click can leave the row
 *   deselected. We therefore resolve the row under the pointer from the event, force it
 *   selected, then trigger the page's own Edit handler by clicking its toolbar button — so
 *   each page keeps its bespoke edit-open logic untouched. If the page has no Edit item
 *   (read-only report/log grids), the click is a harmless no-op.
 */
const GridActionManager = (() => {
    function enableRowDoubleClickEdit(gridObj, editItemId) {
        editItemId = editItemId || 'EditCustom';
        if (!gridObj || !gridObj.element) return;

        gridObj.element.addEventListener('dblclick', (event) => {
            // getRowInfo returns rowData only for a content cell — header / toolbar / pager
            // / group rows / empty space all fall through to a no-op.
            const info = typeof gridObj.getRowInfo === 'function' ? gridObj.getRowInfo(event.target) : null;
            const rowData = info && info.rowData;
            if (!rowData || rowData.id === undefined || rowData.id === null) return;

            const index = gridObj.getRowIndexByPrimaryKey(rowData.id);
            if (index < 0) return;

            // Deterministic selection regardless of the page's select-toggle, so the Edit
            // toolbar item is enabled and acts on this exact row.
            gridObj.clearSelection();
            gridObj.selectRow(index);

            const editButton = document.getElementById(editItemId);
            if (editButton) editButton.click();
        });
    }

    return { enableRowDoubleClickEdit };
})();
