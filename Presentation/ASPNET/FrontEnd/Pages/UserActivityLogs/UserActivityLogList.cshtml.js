const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            filterOpen: false,
            filter: {
                activityType: null,
                fromDate: null,
                toDate: null,
            }
        });

        const mainGridRef = Vue.ref(null);
        const filterPanelRef = Vue.ref(null);
        const filterActivityTypeRef = Vue.ref(null);
        const filterFromDateRef = Vue.ref(null);
        const filterToDateRef = Vue.ref(null);

        // ── List filtering (client-side over the already-fetched list; no API change) ──────────
        const startOfDay = (d) => { const x = new Date(d); x.setHours(0, 0, 0, 0); return x; };
        const endOfDay = (d) => { const x = new Date(d); x.setHours(23, 59, 59, 999); return x; };

        const getFilteredData = () => {
            let data = state.mainData || [];
            const f = state.filter;
            if (f.activityType) data = data.filter(x => x.activityType === f.activityType);
            if (f.fromDate) {
                const from = startOfDay(f.fromDate);
                data = data.filter(x => x.createdAtUtc instanceof Date && x.createdAtUtc >= from);
            }
            if (f.toDate) {
                const to = endOfDay(f.toDate);
                data = data.filter(x => x.createdAtUtc instanceof Date && x.createdAtUtc <= to);
            }
            return data;
        };

        const activeFilterCount = Vue.computed(() => {
            const f = state.filter;
            let count = 0;
            if (f.activityType) count++;
            if (f.fromDate) count++;
            if (f.toDate) count++;
            return count;
        });

        // Re-run the filter and push the result into the grid (keeps the grid's own paging/sorting).
        const applyFilter = () => {
            if (mainGrid.obj) mainGrid.obj.setProperties({ dataSource: getFilteredData() });
        };

        // Distinct activity types found in the loaded list, for the filter dropdown.
        const getActivityTypeOptions = () => {
            const set = new Set();
            (state.mainData || []).forEach(x => { if (x.activityType) set.add(x.activityType); });
            return Array.from(set).sort();
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

        const services = {
            getMainData: async () => {
                return await AxiosManager.get('/UserActivityLog/GetUserActivityLogList', {});
            }
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            }
        };

        // ── Filter-panel controls (live filtering) ────────────────────────────────────────────
        const filterActivityTypeDropdown = {
            obj: null,
            create: () => {
                filterActivityTypeDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: getActivityTypeOptions(),
                    placeholder: 'All types',
                    showClearButton: true,
                    allowFiltering: true,
                    filterType: 'Contains',
                    filterBarPlaceholder: 'Search type',
                    change: (e) => { state.filter.activityType = e.value; applyFilter(); }
                });
                filterActivityTypeDropdown.obj.appendTo(filterActivityTypeRef.value);
            },
            refreshData: () => {
                if (filterActivityTypeDropdown.obj) {
                    filterActivityTypeDropdown.obj.setProperties({ dataSource: getActivityTypeOptions() });
                }
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
            filterActivityTypeDropdown.create();
            filterFromDatePicker.create();
            filterToDatePicker.create();
        };

        const filterHandler = {
            toggle: () => {
                state.filterOpen = !state.filterOpen;
                if (state.filterOpen) Vue.nextTick(positionFilterPanel);
            },
            clear: () => {
                state.filter.activityType = null;
                state.filter.fromDate = null;
                state.filter.toDate = null;
                filterActivityTypeDropdown.obj?.setProperties({ value: null });
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
                    height: '480px',
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
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'userEmail', headerText: 'User Email', width: 220, minWidth: 180 },
                        { field: 'activityType', headerText: 'Activity Type', width: 140, minWidth: 120 },
                        { field: 'description', headerText: 'Description', width: 300, minWidth: 200 },
                        { field: 'pageUrl', headerText: 'Page URL', width: 250, minWidth: 180 },
                        { field: 'ipAddress', headerText: 'IP Address', width: 130, minWidth: 100 },
                        { field: 'userAgent', headerText: 'User Agent', width: 200, minWidth: 150 },
                        { field: 'createdAtUtc', headerText: 'Timestamp (UTC)', width: 175, format: 'dd/MM/yyyy HH:mm:ss' }
                    ],
                    toolbar: [
                        'ExcelExport',
                        { text: 'Filter', tooltipText: 'Show / hide filters', prefixIcon: 'e-filter', id: 'FilterCustom' },
                        'Search'
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['userEmail', 'activityType', 'description', 'pageUrl', 'ipAddress', 'createdAtUtc']);
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport({ fileName: `UserActivityLog_${new Date().toISOString().slice(0, 10)}.xlsx` });
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
                await SecurityManager.authorizePage(['UserActivityLogs']);

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                // Reflect the active-filter count on the toolbar Filter button (primary tint + badge).
                watcherStops.push(Vue.watch(activeFilterCount, (count) => {
                    const el = document.getElementById('FilterCustom');
                    if (!el) return;
                    el.classList.toggle('po-filter-active', count > 0);
                    el.setAttribute('data-filter-count', count);
                }));

                createFilterControls();

                window.addEventListener('resize', onWindowResize);
            } catch (e) {
            }
        });

        Vue.onUnmounted(() => {
            watcherStops.forEach(stop => stop());
            window.removeEventListener('resize', onWindowResize);
            mainGrid.obj?.destroy();
            filterActivityTypeDropdown.obj?.destroy();
            filterFromDatePicker.obj?.destroy();
            filterToDatePicker.obj?.destroy();
        });

        return {
            state,
            mainGridRef,
            filterPanelRef,
            filterActivityTypeRef,
            filterFromDateRef,
            filterToDateRef,
            activeFilterCount,
            handler: {
                toggleFilter: filterHandler.toggle,
                clearFilters: filterHandler.clear
            }
        };
    }
};

Vue.createApp(App).mount('#app');
