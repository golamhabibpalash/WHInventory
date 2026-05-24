const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            filterEntityType: '',
            filterFromDate: '',
            filterToDate: '',
            detail: {
                entityType: '',
                entityId: '',
                operationType: '',
                oldValues: '',
                newValues: '',
                userId: '',
                ipAddress: '',
                createdAtUtc: null
            }
        });

        const mainGridRef = Vue.ref(null);
        const detailModalRef = Vue.ref(null);

        const services = {
            getMainData: async (entityType, fromDate, toDate) => {
                let url = '/AuditLog/GetAuditLogList?';
                if (entityType) url += `entityType=${encodeURIComponent(entityType)}&`;
                if (fromDate) url += `fromDate=${encodeURIComponent(fromDate)}&`;
                if (toDate) url += `toDate=${encodeURIComponent(toDate + 'T23:59:59')}&`;
                return await AxiosManager.get(url, {});
            }
        };

        const formatDate = (val) => {
            if (!val) return '';
            const d = new Date(val);
            return d.toISOString().replace('T', ' ').substring(0, 19);
        };

        const formatJson = (val) => {
            if (!val) return '—';
            try { return JSON.stringify(JSON.parse(val), null, 2); } catch { return val; }
        };

        const operationBadgeClass = (op) => {
            if (op === 'Create') return 'badge bg-success';
            if (op === 'Update') return 'badge bg-warning text-dark';
            if (op === 'Delete') return 'badge bg-danger';
            return 'badge bg-secondary';
        };

        const methods = {
            populateMainData: async (entityType, fromDate, toDate) => {
                try {
                    const response = await services.getMainData(entityType, fromDate, toDate);
                    state.mainData = response?.data?.content?.data ?? [];
                    mainGrid.refresh();
                } catch (error) {
                    console.error('Error loading audit logs:', error);
                }
            }
        };

        const handler = {
            applyFilter: async () => {
                await methods.populateMainData(state.filterEntityType, state.filterFromDate, state.filterToDate);
            },
            clearFilter: async () => {
                state.filterEntityType = '';
                state.filterFromDate = '';
                state.filterToDate = '';
                await methods.populateMainData();
            }
        };

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
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'entityType', headerText: 'Entity Type', width: 160, minWidth: 140 },
                        { field: 'entityId', headerText: 'Entity Id', width: 130, minWidth: 100 },
                        {
                            field: 'operationType', headerText: 'Operation', width: 110, minWidth: 100,
                            template: (row) => {
                                const cls = row.operationType === 'Create' ? 'badge bg-success'
                                    : row.operationType === 'Update' ? 'badge bg-warning text-dark'
                                    : row.operationType === 'Delete' ? 'badge bg-danger'
                                    : 'badge bg-secondary';
                                return `<span class="${cls}">${row.operationType ?? ''}</span>`;
                            }
                        },
                        { field: 'userId', headerText: 'User Id', width: 220, minWidth: 160 },
                        { field: 'ipAddress', headerText: 'IP Address', width: 130, minWidth: 100 },
                        { field: 'createdAtUtc', headerText: 'Timestamp (UTC)', width: 175, format: 'yyyy-MM-dd HH:mm:ss' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'View Detail', tooltipText: 'View Detail', prefixIcon: 'e-zoom-in', id: 'ViewDetailCustom' }
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['ViewDetailCustom'], false);
                        mainGrid.obj.autoFitColumns(['entityType', 'operationType', 'userId', 'ipAddress', 'createdAtUtc']);
                    },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['ViewDetailCustom'], mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['ViewDetailCustom'], mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) mainGrid.obj.clearSelection();
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport({ fileName: `AuditLog_${new Date().toISOString().slice(0,10)}.xlsx` });
                        }
                        if (args.item.id === 'ViewDetailCustom') {
                            const rec = mainGrid.obj.getSelectedRecords()[0];
                            if (rec) {
                                state.detail.entityType = rec.entityType ?? '';
                                state.detail.entityId = rec.entityId ?? '';
                                state.detail.operationType = rec.operationType ?? '';
                                state.detail.oldValues = rec.oldValues ?? '';
                                state.detail.newValues = rec.newValues ?? '';
                                state.detail.userId = rec.userId ?? '';
                                state.detail.ipAddress = rec.ipAddress ?? '';
                                state.detail.createdAtUtc = rec.createdAtUtc;
                                detailModal.obj.show();
                            }
                        }
                    }
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        const detailModal = {
            obj: null,
            create: () => {
                detailModal.obj = new bootstrap.Modal(detailModalRef.value, { backdrop: 'static', keyboard: false });
            }
        };

        Vue.onMounted(async () => {
            SecurityManager.authorizePage(['AuditLogs']);
            mainGrid.create([]);
            detailModal.create();
            await methods.populateMainData();
        });

        return { state, mainGridRef, detailModalRef, formatDate, formatJson, operationBadgeClass, handler };
    }
};

Vue.createApp(App).mount('#app');
