const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            dateFrom: null,
            dateTo: null,
        });

        const mainGridRef = Vue.ref(null);
        const dateFromRef = Vue.ref(null);
        const dateToRef = Vue.ref(null);

        const dateFromPicker = {
            obj: null,
            create: () => {
                dateFromPicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'Select From Date',
                    format: 'yyyy-MM-dd',
                    change: (args) => {
                        state.dateFrom = args.value ? args.value : null;
                    }
                });
                dateFromPicker.obj.appendTo(dateFromRef.value);
            },
            clear: () => {
                dateFromPicker.obj.value = null;
                state.dateFrom = null;
            }
        };

        const dateToPicker = {
            obj: null,
            create: () => {
                dateToPicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'Select To Date',
                    format: 'yyyy-MM-dd',
                    change: (args) => {
                        state.dateTo = args.value ? args.value : null;
                    }
                });
                dateToPicker.obj.appendTo(dateToRef.value);
            },
            clear: () => {
                dateToPicker.obj.value = null;
                state.dateTo = null;
            }
        };

        const services = {
            getMainData: async (dateFrom, dateTo) => {
                try {
                    const params = {};
                    if (dateFrom) params.dateFrom = dateFrom.toISOString();
                    if (dateTo) params.dateTo = dateTo.toISOString();
                    const response = await AxiosManager.get('/PurchaseOrderItem/GetPurchaseOrderItemList', params);
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateMainData: async (dateFrom, dateTo) => {
                const response = await services.getMainData(dateFrom, dateTo);
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: item.orderDate ? new Date(item.orderDate) : null,
                    createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null,
                }));
            },
        };

        const handler = {
            applyFilter: async () => {
                await methods.populateMainData(state.dateFrom, state.dateTo);
                mainGrid.refresh();
            },
            clearFilter: async () => {
                dateFromPicker.clear();
                dateToPicker.clear();
                await methods.populateMainData(null, null);
                mainGrid.refresh();
            },
        };

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
                        columns: ['purchaseOrderNumber']
                    },
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'purchaseOrderNumber', direction: 'Descending' }] },
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
                        { field: 'vendorName', headerText: 'Vendor', width: 200, minWidth: 200 },
                        { field: 'purchaseOrderNumber', headerText: 'Purchase Order', width: 200, minWidth: 200 },
                        { field: 'orderDate', headerText: 'Order Date', width: 130, minWidth: 130, format: 'yyyy-MM-dd', type: 'date' },
                        { field: 'productNumber', headerText: 'Product Number', width: 200, minWidth: 200 },
                        { field: 'productName', headerText: 'Product Name', width: 200, minWidth: 200 },
                        { field: 'unitPrice', headerText: 'Unit Price', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'quantity', headerText: 'Quantity', width: 150, minWidth: 150 },
                        { field: 'total', headerText: 'Total', width: 150, minWidth: 150, format: 'N2' },
                    ],
                    aggregates: [
                        {
                            columns: [
                                {
                                    type: 'Sum',
                                    field: 'total',
                                    groupCaptionTemplate: 'Total: ${Sum}',
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
                        mainGrid.obj.autoFitColumns(['vendorName', 'purchaseOrderNumber', 'orderDate', 'productNumber', 'productName', 'unitPrice', 'quantity', 'total']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => { },
                    rowDeselected: () => { },
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
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PurchaseReports']);
                await SecurityManager.validateToken();
                await methods.populateMainData(null, null);
                await mainGrid.create(state.mainData);
                dateFromPicker.create();
                dateToPicker.create();
            } catch (e) {
                console.error('page init error:', e);
            } finally {

            }
        });

        return {
            mainGridRef,
            dateFromRef,
            dateToRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');
