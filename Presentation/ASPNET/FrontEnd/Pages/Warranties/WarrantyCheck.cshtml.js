const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            customerListLookupData: [],
            productListLookupData: [],
            customerId: null,
            productId: null,
            claimDate: new Date().toISOString().slice(0, 10),
            isSearching: false,
        });

        const mainGridRef = Vue.ref(null);
        const customerIdRef = Vue.ref(null);
        const productIdRef = Vue.ref(null);
        const claimDateRef = Vue.ref(null);

        const services = {
            getCustomerListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Customer/GetCustomerList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Product/GetProductList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getWarrantyCheck: async (customerId, productId, claimDate) => {
                try {
                    const params = {};
                    if (customerId) params.customerId = customerId;
                    if (productId) params.productId = productId;
                    if (claimDate) params.claimDate = claimDate;
                    const response = await AxiosManager.get('/Warranty/GetWarrantyCheck', { params });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateCustomerListLookupData: async () => {
                const response = await services.getCustomerListLookupData();
                state.customerListLookupData = response?.data?.content?.data ?? [];
                if (customerListLookup.obj) {
                    customerListLookup.obj.setProperties({ dataSource: state.customerListLookupData });
                }
            },
            populateProductListLookupData: async () => {
                const response = await services.getProductListLookupData();
                state.productListLookupData = (response?.data?.content?.data ?? [])
                    .filter(p => p.isWarrantyApplicable === true);
                if (productListLookup.obj) {
                    productListLookup.obj.setProperties({ dataSource: state.productListLookupData });
                }
            },
        };

        const customerListLookup = {
            obj: null,
            create: () => {
                customerListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.customerListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: '-- All Customers --',
                    popupHeight: '200px',
                    allowFiltering: true,
                    showClearButton: true,
                    change: (e) => {
                        state.customerId = e.value ?? null;
                    }
                });
                customerListLookup.obj.appendTo(customerIdRef.value);
            },
        };

        const productListLookup = {
            obj: null,
            create: () => {
                productListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: '-- All Warranty Products --',
                    popupHeight: '200px',
                    allowFiltering: true,
                    showClearButton: true,
                    change: (e) => {
                        state.productId = e.value ?? null;
                    }
                });
                productListLookup.obj.appendTo(productIdRef.value);
            },
        };

        const claimDatePicker = {
            obj: null,
            create: () => {
                claimDatePicker.obj = new ej.calendars.DatePicker({
                    value: new Date(),
                    format: 'yyyy-MM-dd',
                    placeholder: 'Select claim date',
                    change: (e) => {
                        state.claimDate = e.value ? e.value.toISOString().slice(0, 10) : null;
                    }
                });
                claimDatePicker.obj.appendTo(claimDateRef.value);
            },
        };

        const handler = {
            search: async () => {
                try {
                    state.isSearching = true;
                    const response = await services.getWarrantyCheck(state.customerId, state.productId, state.claimDate);
                    state.mainData = (response?.data?.content?.data ?? []).map(item => ({
                        ...item,
                        deliveryDate: item.deliveryDate ? new Date(item.deliveryDate) : null,
                        warrantyExpireDate: item.warrantyExpireDate ? new Date(item.warrantyExpireDate) : null,
                    }));
                    mainGrid.refresh();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Search Failed',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSearching = false;
                }
            },
            reset: () => {
                state.customerId = null;
                state.productId = null;
                state.claimDate = new Date().toISOString().slice(0, 10);
                customerListLookup.obj.value = null;
                productListLookup.obj.value = null;
                claimDatePicker.obj.value = new Date();
                state.mainData = [];
                mainGrid.refresh();
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Warranties']);
                await SecurityManager.validateToken();

                await Promise.all([
                    methods.populateCustomerListLookupData(),
                    methods.populateProductListLookupData(),
                ]);

                customerListLookup.create();
                productListLookup.create();
                claimDatePicker.create();
                mainGrid.create(state.mainData);

            } catch (e) {
            } finally {
            }
        });

        const mainGrid = {
            obj: null,
            create: (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '350px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'deliveryDate', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'deliveryOrderId', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'deliveryOrderNumber', headerText: 'Delivery No.', width: 150, minWidth: 150 },
                        { field: 'deliveryDate', headerText: 'Delivery Date', width: 140, minWidth: 140, format: 'yyyy-MM-dd' },
                        { field: 'customerName', headerText: 'Customer', width: 180, minWidth: 180 },
                        { field: 'salesOrderNumber', headerText: 'Sales Order No.', width: 150, minWidth: 150 },
                        { field: 'productNumber', headerText: 'Product No.', width: 130, minWidth: 130 },
                        { field: 'productName', headerText: 'Product', width: 200, minWidth: 200 },
                        { field: 'quantity', headerText: 'Qty', width: 80, minWidth: 80, format: 'N2', textAlign: 'Right' },
                        { field: 'warrantyDays', headerText: 'Warranty (Days)', width: 140, minWidth: 140, textAlign: 'Right' },
                        { field: 'warrantyExpireDate', headerText: 'Expires On', width: 130, minWidth: 130, format: 'yyyy-MM-dd' },
                        { field: 'daysRemaining', headerText: 'Days Remaining', width: 140, minWidth: 140, textAlign: 'Right' },
                        {
                            field: 'warrantyStatus', headerText: 'Warranty Status', width: 140, minWidth: 140, textAlign: 'Center',
                            template: (data) => {
                                const cls = data.warrantyStatus === 'Valid' ? 'badge bg-success'
                                          : data.warrantyStatus === 'Expired' ? 'badge bg-danger'
                                          : 'badge bg-secondary';
                                return `<span class="${cls}">${data.warrantyStatus ?? ''}</span>`;
                            }
                        },
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['deliveryOrderNumber', 'deliveryDate', 'customerName', 'salesOrderNumber', 'productName', 'warrantyStatus', 'warrantyExpireDate']);
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

        return {
            mainGridRef,
            customerIdRef,
            productIdRef,
            claimDateRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');
