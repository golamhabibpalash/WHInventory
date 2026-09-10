const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            scopeLabel: 'All warehouses'
        });

        const mainGridRef = Vue.ref(null);

        const warehouseId = new URLSearchParams(window.location.search).get('warehouseId') ?? '';

        const services = {
            getMainData: async () => {
                try {
                    const params = new URLSearchParams({ transType: 'In' });
                    if (warehouseId) params.set('warehouseId', warehouseId);
                    const response = await AxiosManager.get(`/InventoryTransaction/GetInventoryTransactionList?${params}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getWarehouseList: async () => {
                try {
                    const response = await AxiosManager.get('/Warehouse/GetWarehouseList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateScopeLabel: async () => {
                if (!warehouseId) {
                    return;
                }

                const response = await services.getWarehouseList();
                const list = response?.data?.content?.data ?? [];
                const match = list.find(x => x.id === warehouseId);
                state.scopeLabel = match ? `Warehouse: ${match.name}` : 'Selected warehouse';
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data ?? [];
            },
            formatQty: (value) => {
                const number = Number(value) || 0;
                return number.toLocaleString(undefined, { maximumFractionDigits: 2 });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['StockReports']);
                await SecurityManager.validateToken();

                await methods.populateScopeLabel().catch(e => {
                    console.error('Warehouse scope label failed to load:', e);
                });
                await methods.populateMainData();
                await mainGrid.create(state.mainData);

            } catch (e) {
            } finally {

            }
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '240px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: true,
                    groupSettings: {
                        columns: ['warehouseName']
                    },
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'movementDate', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'movementDate', headerText: 'Date', width: 120, format: 'dd/MM/yyyy' },
                        { field: 'moduleCode', headerText: 'Type', width: 120 },
                        { field: 'moduleNumber', headerText: 'Document No', width: 160 },
                        { field: 'warehouseName', headerText: 'Warehouse', width: 180 },
                        { field: 'productName', headerText: 'Product', width: 220 },
                        { field: 'movement', headerText: 'Qty', width: 100, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'warehouseFromName', headerText: 'From', width: 160 },
                        { field: 'warehouseToName', headerText: 'To', width: 160 },
                        { field: 'createdAtUtc', headerText: 'Created', width: 140, format: 'dd/MM/yyyy HH:mm' }
                    ],
                    aggregates: [
                        {
                            columns: [
                                {
                                    type: 'Sum',
                                    field: 'movement',
                                    groupCaptionTemplate: 'Qty: ${Sum}',
                                    format: 'N2'
                                }
                            ]
                        }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['moduleCode', 'movementDate', 'createdAtUtc']);
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
                            mainGrid.obj.excelExport();
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
                GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        return {
            mainGridRef,
            state,
        };
    }
};

Vue.createApp(App).mount('#app');
