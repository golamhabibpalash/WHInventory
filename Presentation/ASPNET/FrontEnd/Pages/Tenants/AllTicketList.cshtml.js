const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            tenantId: null,
            statusId: null,
            search: '',
            overdueOnly: false,
        });

        const mainGridRef = Vue.ref(null);
        const tenantRef = Vue.ref(null);
        const statusRef = Vue.ref(null);

        const STATUS_LABELS = { 0: 'New', 1: 'Open', 2: 'In Progress', 3: 'Pending User', 4: 'Pending Internal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_KEYS = { 0: 'New', 1: 'Open', 2: 'InProgress', 3: 'PendingUser', 4: 'PendingInternal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_OPTIONS = Object.keys(STATUS_LABELS).map(k => ({ id: Number(k), name: STATUS_LABELS[k] }));

        // AxiosManager.get(url, config) only forwards config.headers/responseType — it does not
        // build a query string from config.params, so query parameters go straight into the URL.
        const toQueryString = (params) => {
            const usp = new URLSearchParams();
            Object.entries(params || {}).forEach(([k, v]) => {
                if (v !== null && v !== undefined && v !== '') usp.append(k, v);
            });
            const qs = usp.toString();
            return qs ? `?${qs}` : '';
        };

        const services = {
            getTicketList: async (params) => AxiosManager.get('/Ticket/GetAllTenantsTicketList' + toQueryString(params)),
            getTenantList: async () => AxiosManager.get('/Tenant/GetTenantList', {}),
        };

        const buildParams = () => {
            const params = { pageSize: 2000 };
            if (state.tenantId) params.tenantId = state.tenantId;
            if (state.statusId !== null && state.statusId !== undefined) params.status = state.statusId;
            if (state.search) params.search = state.search;
            if (state.overdueOnly) params.overdueOnly = true;
            return params;
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getTicketList(buildParams());
                const data = response?.data?.content?.data ?? [];
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
                state.tenantId = null;
                state.statusId = null;
                state.search = '';
                state.overdueOnly = false;
                tenantDropdown.obj?.setProperties({ value: null });
                statusDropdown.obj?.setProperties({ value: null });
                methods.populateMainData();
            },
            toggleOverdue: () => {
                state.overdueOnly = !state.overdueOnly;
                methods.populateMainData();
            },
        };

        const tenantDropdown = {
            obj: null,
            create: async () => {
                const response = await services.getTenantList();
                tenantDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: response?.data?.content?.data ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Tenants',
                    change: (e) => { state.tenantId = e.value; }
                });
                tenantDropdown.obj.appendTo(tenantRef.value);
            }
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

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Tenants']);
                await SecurityManager.validateToken();

                await tenantDropdown.create();
                statusDropdown.create();

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
                        { field: 'tenantName', headerText: 'Tenant', width: 150 },
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
                                ? '<span class="ticket-overdue-badge"><i class="fas fa-exclamation-circle me-1"></i>Overdue</span>'
                                : '<span class="text-muted small">&mdash;</span>';
                        }
                    },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['ticketNumber', 'tenantName', 'requesterName', 'categoryName', 'priorityName', 'status', 'assignedToName', 'createdAtUtc', 'lastActivityAtUtc', 'isOverdue']);
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

        return { mainGridRef, tenantRef, statusRef, state, handler };
    }
};

Vue.createApp(App).mount('#app');
