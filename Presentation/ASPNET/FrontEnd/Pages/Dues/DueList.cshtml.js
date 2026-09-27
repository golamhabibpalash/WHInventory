const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            filterOpen: false,
            filter: {
                customerId: null,
                dueStatus: '',
                fromDate: null,
                toDate: null,
            },
            customerListLookupData: [],
        });

        const mainGridRef = Vue.ref(null);
        const filterPanelRef = Vue.ref(null);
        const filterCustomerRef = Vue.ref(null);
        const filterStatusRef = Vue.ref(null);
        const filterFromDateRef = Vue.ref(null);
        const filterToDateRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Customer/GetCustomerDueList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Customer/GetCustomerList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateCustomerListLookupData: async () => {
                const response = await services.getCustomerListLookupData();
                const data = response?.data?.content?.data ?? [];
                // Precompute the "Name - Mobile" label the dropdown shows.
                // Raw name/phoneNumber are preserved on each item so the search still matches both.
                state.customerListLookupData = data.map(c => ({
                    ...c,
                    displayName: c.phoneNumber ? `${c.name} - ${c.phoneNumber}` : c.name
                }));
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                const rows = response?.data?.content?.data ?? [];
                state.mainData = rows.map(row => ({
                    ...row,
                    lastOrderDate: row.lastOrderDate ? new Date(row.lastOrderDate) : null,
                }));
            },
        };

        // ── List filtering (client-side over the already-fetched list; no API change) ──────────
        const startOfDay = (d) => { const x = new Date(d); x.setHours(0, 0, 0, 0); return x; };
        const endOfDay = (d) => { const x = new Date(d); x.setHours(23, 59, 59, 999); return x; };

        const getFilteredData = () => {
            let data = state.mainData || [];
            const f = state.filter;
            if (f.customerId) data = data.filter(x => x.customerId === f.customerId);
            if (f.dueStatus) data = data.filter(x => x.dueStatus === f.dueStatus);
            if (f.fromDate) {
                const from = startOfDay(f.fromDate);
                data = data.filter(x => x.lastOrderDate instanceof Date && x.lastOrderDate >= from);
            }
            if (f.toDate) {
                const to = endOfDay(f.toDate);
                data = data.filter(x => x.lastOrderDate instanceof Date && x.lastOrderDate <= to);
            }
            return data;
        };

        const activeFilterCount = Vue.computed(() => {
            const f = state.filter;
            let count = 0;
            if (f.customerId) count++;
            if (f.dueStatus) count++;
            if (f.fromDate) count++;
            if (f.toDate) count++;
            return count;
        });

        const listStats = Vue.computed(() => {
            const rows = getFilteredData();
            return {
                total: rows.length,
                withDue: rows.filter(row => (row.dueAmount || 0) > 0.005).length,
                billed: rows.reduce((sum, row) => sum + (row.billedAmount || 0), 0),
                due: rows.reduce((sum, row) => sum + (row.dueAmount || 0), 0)
            };
        });

        // Re-run the filter and push the result into the grid (keeps the grid's own paging/sorting).
        const applyFilter = () => {
            if (mainGrid.obj) mainGrid.obj.setProperties({ dataSource: getFilteredData() });
        };

        // Drop the panel directly beneath the grid toolbar, spanning the grid width.
        const positionFilterPanel = () => {
            const wrap = mainGridRef.value?.closest('.po-grid-wrap');
            const toolbar = mainGridRef.value?.querySelector('.e-toolbar');
            if (!wrap || !toolbar || !filterPanelRef.value) return;
            const top = toolbar.getBoundingClientRect().bottom - wrap.getBoundingClientRect().top;
            filterPanelRef.value.style.top = `${top}px`;
        };

        const onWindowResize = () => { if (state.filterOpen) positionFilterPanel(); };

        const filterCustomerDropdown = {
            obj: null,
            create: () => {
                filterCustomerDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.customerListLookupData ?? [],
                    fields: { value: 'id', text: 'displayName' },
                    placeholder: 'All customers',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    showClearButton: true,
                    filterBarPlaceholder: 'Search by name or mobile',
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        const term = (e.text || '').toLowerCase();
                        const source = state.customerListLookupData ?? [];
                        e.updateData(term ? source.filter(c => (c.name || '').toLowerCase().includes(term) || (c.phoneNumber || '').toLowerCase().includes(term)) : source);
                    },
                    change: (e) => { state.filter.customerId = e.value; applyFilter(); }
                });
                filterCustomerDropdown.obj.appendTo(filterCustomerRef.value);
            }
        };

        const filterStatusDropdown = {
            obj: null,
            create: () => {
                filterStatusDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: [{ id: 'Due', name: 'Due' }, { id: 'Settled', name: 'Settled' }],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All statuses',
                    showClearButton: true,
                    change: (e) => { state.filter.dueStatus = e.value ?? ''; applyFilter(); }
                });
                filterStatusDropdown.obj.appendTo(filterStatusRef.value);
            }
        };

        const filterFromDatePicker = {
            obj: null,
            create: () => {
                filterFromDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'From date',
                    showClearButton: true,
                    change: (e) => { state.filter.fromDate = e.value; applyFilter(); }
                });
                filterFromDatePicker.obj.appendTo(filterFromDateRef.value);
            }
        };

        const filterToDatePicker = {
            obj: null,
            create: () => {
                filterToDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'To date',
                    showClearButton: true,
                    change: (e) => { state.filter.toDate = e.value; applyFilter(); }
                });
                filterToDatePicker.obj.appendTo(filterToDateRef.value);
            }
        };

        const createFilterControls = () => {
            filterCustomerDropdown.create();
            filterStatusDropdown.create();
            filterFromDatePicker.create();
            filterToDatePicker.create();
        };

        const filterHandler = {
            toggle: () => {
                state.filterOpen = !state.filterOpen;
                if (state.filterOpen) Vue.nextTick(positionFilterPanel);
            },
            clear: () => {
                state.filter.customerId = null;
                state.filter.dueStatus = '';
                state.filter.fromDate = null;
                state.filter.toDate = null;
                filterCustomerDropdown.obj?.setProperties({ value: null });
                filterStatusDropdown.obj?.setProperties({ value: null });
                filterFromDatePicker.obj?.setProperties({ value: null });
                filterToDatePicker.obj?.setProperties({ value: null });
                applyFilter();
            }
        };

        const watcherStops = [];

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'dueAmount', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 20, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        {
                            field: 'customerId', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'customerNumber', headerText: 'Customer No.', width: 160, minWidth: 160 },
                        { field: 'customerName', headerText: 'Customer', width: 220, minWidth: 220 },
                        { field: 'phoneNumber', headerText: 'Phone', width: 160, minWidth: 160 },
                        { field: 'orderCount', headerText: 'Orders', width: 110, minWidth: 110, type: 'number', textAlign: 'Right' },
                        { field: 'lastOrderDate', headerText: 'Last Order', width: 140, minWidth: 140, type: 'date', format: 'dd/MM/yyyy' },
                        { field: 'billedAmount', headerText: 'Billed', width: 150, minWidth: 150, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'paidAmount', headerText: 'Paid', width: 150, minWidth: 150, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'dueAmount', headerText: 'Due', width: 150, minWidth: 150, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'dueStatus', headerText: 'Status', width: 120, minWidth: 120 }
                    ],
                    toolbar: [
                        'ExcelExport',
                        { text: 'Filter', tooltipText: 'Show / hide filters', prefixIcon: 'e-filter', id: 'FilterCustom' },
                        'Search',
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['customerNumber', 'customerName', 'phoneNumber', 'orderCount', 'lastOrderDate', 'billedAmount', 'paidAmount', 'dueAmount', 'dueStatus']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                        } else {
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                        } else {
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            const date = new Date().toISOString().slice(0, 10);
                            mainGrid.obj.excelExport({ fileName: `CustomerDues_${date}.xlsx` });
                        }

                        if (args.item.id === 'FilterCustom') {
                            filterHandler.toggle();
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
                GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
            },
            refresh: () => {
                // Preserve any active filter selection when the underlying list is reloaded.
                mainGrid.obj.setProperties({ dataSource: getFilteredData() });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Dues']);
                await SecurityManager.validateToken();

                await methods.populateCustomerListLookupData();
                await methods.populateMainData();
                await mainGrid.create(getFilteredData());

                // Reflect the active-filter count on the toolbar Filter button (primary tint + badge).
                watcherStops.push(Vue.watch(activeFilterCount, (count) => {
                    const el = document.getElementById('FilterCustom');
                    if (!el) return;
                    el.classList.toggle('po-filter-active', count > 0);
                    el.setAttribute('data-filter-count', count);
                }));
                window.addEventListener('resize', onWindowResize);

                createFilterControls();
            } catch (e) {
            } finally {

            }
        });

        Vue.onUnmounted(() => {
            watcherStops.forEach(stop => stop());
            window.removeEventListener('resize', onWindowResize);
            mainGrid.obj?.destroy();
            filterCustomerDropdown.obj?.destroy();
            filterStatusDropdown.obj?.destroy();
            filterFromDatePicker.obj?.destroy();
            filterToDatePicker.obj?.destroy();
        });

        return {
            mainGridRef,
            filterPanelRef,
            filterCustomerRef,
            filterStatusRef,
            filterFromDateRef,
            filterToDateRef,
            activeFilterCount,
            listStats,
            state,
            handler: {
                toggleFilter: filterHandler.toggle,
                clearFilters: filterHandler.clear,
                formatAmount: (value) => NumberFormatManager.formatToLocale(value ?? 0),
            },
        };
    }
};

Vue.createApp(App).mount('#app');
