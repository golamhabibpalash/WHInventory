const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            statusId: null,
            priorityId: null,
            categoryId: null,
            assignedToId: null,
            fromDate: null,
            toDate: null,
            quickFilter: null, // 'unassigned' | 'myAssigned' | 'overdue' | 'critical'
        });

        const mainGridRef = Vue.ref(null);
        const statusRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);
        const categoryRef = Vue.ref(null);
        const assigneeRef = Vue.ref(null);
        const fromDateRef = Vue.ref(null);
        const toDateRef = Vue.ref(null);

        const STATUS_LABELS = { 0: 'New', 1: 'Open', 2: 'In Progress', 3: 'Pending User', 4: 'Pending Internal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_KEYS = { 0: 'New', 1: 'Open', 2: 'InProgress', 3: 'PendingUser', 4: 'PendingInternal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_OPTIONS = Object.keys(STATUS_LABELS).map(k => ({ id: Number(k), name: STATUS_LABELS[k] }));

        // AxiosManager.get(url, config) only forwards config.headers/responseType — it does not
        // build a query string from config.params (see wwwroot/lib/indotalent/axios-manager.js),
        // so every GET with query parameters has to build its own query string into the URL.
        const toQueryString = (params) => {
            const usp = new URLSearchParams();
            Object.entries(params || {}).forEach(([k, v]) => {
                if (v !== null && v !== undefined && v !== '') usp.append(k, v);
            });
            const qs = usp.toString();
            return qs ? `?${qs}` : '';
        };

        const services = {
            getTicketList: async (params) => AxiosManager.get('/Ticket/GetTicketList' + toQueryString(params)),
            getCategoryList: async () => AxiosManager.get('/TicketCategory/GetTicketCategoryList', {}),
            getPriorityList: async () => AxiosManager.get('/TicketPriority/GetTicketPriorityList', {}),
            getUserList: async () => AxiosManager.get('/Security/GetUserList', {}),
        };

        const buildParams = () => {
            const params = { pageSize: 2000 };
            if (state.statusId !== null && state.statusId !== undefined) params.status = state.statusId;
            if (state.priorityId) params.priorityId = state.priorityId;
            if (state.categoryId) params.categoryId = state.categoryId;
            if (state.assignedToId) params.assignedToId = state.assignedToId;
            if (state.fromDate) params.fromDateUtc = new Date(state.fromDate).toISOString();
            if (state.toDate) params.toDateUtc = new Date(state.toDate).toISOString();
            if (state.quickFilter === 'unassigned') params.unassignedOnly = true;
            if (state.quickFilter === 'overdue') params.overdueOnly = true;
            if (state.quickFilter === 'myAssigned') params.assignedToId = StorageManager.getUserId();
            // 'critical' quick filter is applied client-side after fetch (Level isn't known to the
            // filter bar's priority dropdown by name alone) — see methods.populateMainData.
            return params;
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getTicketList(buildParams());
                let data = response?.data?.content?.data ?? [];
                if (state.quickFilter === 'critical') {
                    data = data.filter(x => (x.priorityName || '').toLowerCase() === 'critical');
                }
                state.mainData = data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc),
                    lastActivityAtUtc: item.lastActivityAtUtc ? new Date(item.lastActivityAtUtc) : null,
                }));
                mainGrid.refresh();
            },
        };

        const handler = {
            search: () => methods.populateMainData(),
            clearFilters: () => {
                state.statusId = null;
                state.priorityId = null;
                state.categoryId = null;
                state.assignedToId = null;
                state.fromDate = null;
                state.toDate = null;
                state.quickFilter = null;
                statusDropdown.obj?.setProperties({ value: null });
                priorityDropdown.obj?.setProperties({ value: null });
                categoryDropdown.obj?.setProperties({ value: null });
                assigneeDropdown.obj?.setProperties({ value: null });
                fromDatePicker.obj?.setProperties({ value: null });
                toDatePicker.obj?.setProperties({ value: null });
                methods.populateMainData();
            },
            toggleQuickFilter: (name) => {
                state.quickFilter = state.quickFilter === name ? null : name;
                methods.populateMainData();
            },
        };

        const statusDropdown = {
            obj: null,
            create: () => {
                statusDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: STATUS_OPTIONS,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Statuses',
                    change: (e) => { state.statusId = e.value; }
                });
                statusDropdown.obj.appendTo(statusRef.value);
            }
        };

        const priorityDropdown = {
            obj: null,
            create: async () => {
                const response = await services.getPriorityList();
                priorityDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: response?.data?.content?.data ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Priorities',
                    change: (e) => { state.priorityId = e.value; }
                });
                priorityDropdown.obj.appendTo(priorityRef.value);
            }
        };

        const categoryDropdown = {
            obj: null,
            create: async () => {
                const response = await services.getCategoryList();
                categoryDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: response?.data?.content?.data ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Categories',
                    change: (e) => { state.categoryId = e.value; }
                });
                categoryDropdown.obj.appendTo(categoryRef.value);
            }
        };

        const assigneeDropdown = {
            obj: null,
            create: async () => {
                const response = await services.getUserList();
                assigneeDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: response?.data?.content?.data ?? [],
                    fields: { value: 'id', text: 'email' },
                    placeholder: 'All Agents',
                    change: (e) => { state.assignedToId = e.value; }
                });
                assigneeDropdown.obj.appendTo(assigneeRef.value);
            }
        };

        const fromDatePicker = {
            obj: null,
            create: () => {
                fromDatePicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'From date',
                    format: 'dd/MM/yyyy',
                    change: (e) => { state.fromDate = e.value; }
                });
                fromDatePicker.obj.appendTo(fromDateRef.value);
            }
        };
        const toDatePicker = {
            obj: null,
            create: () => {
                toDatePicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'To date',
                    format: 'dd/MM/yyyy',
                    change: (e) => { state.toDate = e.value; }
                });
                toDatePicker.obj.appendTo(toDateRef.value);
            }
        };

        const applyDrillDownFromQuery = () => {
            const params = new URLSearchParams(window.location.search);
            const status = params.get('status');
            if (status !== null) {
                state.statusId = Number(status);
                statusDropdown.obj?.setProperties({ value: Number(status) });
            }
            if (params.get('overdue') === 'true') { state.quickFilter = 'overdue'; }
            if (params.get('unassigned') === 'true') { state.quickFilter = 'unassigned'; }
            if (params.get('critical') === 'true') { state.quickFilter = 'critical'; }
            if (params.get('myAssigned') === 'true') { state.quickFilter = 'myAssigned'; }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TicketAgent']);
                await SecurityManager.validateToken();

                statusDropdown.create();
                await Promise.all([priorityDropdown.create(), categoryDropdown.create(), assigneeDropdown.create()]);
                fromDatePicker.create();
                toDatePicker.create();

                applyDrillDownFromQuery();

                await mainGrid.create([]);
                await methods.populateMainData();
            } catch (e) {
            }
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '520px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'lastActivityAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 20, pageSizes: ["10", "20", "50", "100", "All"] },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'ticketNumber', headerText: 'Ticket #', width: 130 },
                        { field: 'subject', headerText: 'Subject', width: 240, minWidth: 240 },
                        { field: 'requesterName', headerText: 'Requester', width: 150 },
                        { field: 'categoryName', headerText: 'Category', width: 130 },
                        { field: 'priorityName', headerText: 'Priority', width: 110 },
                        { field: 'status', headerText: 'Status', width: 140 },
                        { field: 'assignedToName', headerText: 'Assigned To', width: 150 },
                        { field: 'createdAtUtc', headerText: 'Created', width: 130, format: 'dd/MM/yyyy' },
                        { field: 'lastActivityAtUtc', headerText: 'Last Activity', width: 150, format: 'dd/MM/yyyy HH:mm' },
                        { field: 'isOverdue', headerText: 'SLA', width: 100, textAlign: 'Center' },
                    ],
                    queryCellInfo: (args) => {
                        if (args.column.field === 'status') {
                            const key = STATUS_KEYS[args.data.status] ?? 'New';
                            const label = STATUS_LABELS[args.data.status] ?? 'New';
                            args.cell.innerHTML = `<span class="ticket-status-badge ticket-status-${key}">${label}</span>`;
                        }
                        if (args.column.field === 'priorityName' && args.data.priorityName) {
                            const color = args.data.priorityColorHex || '#6c757d';
                            args.cell.innerHTML = `<span class="ticket-priority-badge"><span class="ticket-priority-dot" style="background:${color};"></span>${args.data.priorityName}</span>`;
                        }
                        if (args.column.field === 'assignedToName' && !args.data.assignedToName) {
                            args.cell.innerHTML = '<span class="text-muted small">Unassigned</span>';
                        }
                        if (args.column.field === 'isOverdue') {
                            args.cell.innerHTML = args.data.isOverdue
                                ? '<span class="ticket-overdue-badge"><i class="fas fa-exclamation-circle"></i>Overdue</span>'
                                : '<span class="text-muted small">&mdash;</span>';
                        }
                    },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['ticketNumber', 'requesterName', 'categoryName', 'priorityName', 'status', 'assignedToName', 'createdAtUtc', 'lastActivityAtUtc', 'isOverdue']);
                    },
                    excelExportComplete: () => { },
                    recordClick: (args) => {
                        if (args.rowData?.id) {
                            window.location.href = `/Tickets/TicketDetails?id=${args.rowData.id}`;
                        }
                    },
                    toolbar: ['ExcelExport', 'Search'],
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') mainGrid.obj.excelExport();
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
                GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj?.setProperties({ dataSource: state.mainData });
            }
        };

        return { mainGridRef, statusRef, priorityRef, categoryRef, assigneeRef, fromDateRef, toDateRef, state, handler };
    }
};

Vue.createApp(App).mount('#app');
