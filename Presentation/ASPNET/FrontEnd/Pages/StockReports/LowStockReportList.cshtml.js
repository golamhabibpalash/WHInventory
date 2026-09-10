const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            thresholdLabel: '',
            scopeLabel: 'All warehouses'
        });

        const mainGridRef = Vue.ref(null);

        // The tile on the dashboard forwards its selected branch as ?warehouseId=...
        // so the drill-down list matches the count the user clicked.
        const warehouseId = new URLSearchParams(window.location.search).get('warehouseId') ?? '';

        const services = {
            getMainData: async () => {
                try {
                    const query = warehouseId ? `?warehouseId=${encodeURIComponent(warehouseId)}` : '';
                    const response = await AxiosManager.get(`/Dashboard/GetLowStockProductList${query}`, {});
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
                const rows = response?.data?.content?.data ?? [];
                state.mainData = rows;
                state.thresholdLabel = methods.formatQty(rows[0]?.lowStockThreshold ?? 0);
            },
            formatQty: (value) => {
                const number = Number(value) || 0;
                return number.toLocaleString(undefined, { maximumFractionDigits: 0 });
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
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'stockOnHand', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'productId', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'productNumber', headerText: 'Product Number', width: 180 },
                        { field: 'productName', headerText: 'Product Name', width: 220 },
                        { field: 'productGroupName', headerText: 'Product Group', width: 170 },
                        { field: 'brandName', headerText: 'Brand', width: 150 },
                        { field: 'unitMeasureName', headerText: 'Unit', width: 110 },
                        { field: 'stockOnHand', headerText: 'Stock On Hand', width: 150, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'lowStockThreshold', headerText: 'Threshold', width: 120, type: 'number', format: 'N2', textAlign: 'Right' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['productNumber', 'unitMeasureName', 'stockOnHand', 'lowStockThreshold']);
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
