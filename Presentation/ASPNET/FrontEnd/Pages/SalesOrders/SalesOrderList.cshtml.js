const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            customerListLookupData: [],
            taxListLookupData: [],
            salesOrderStatusListLookupData: [],
            secondaryData: [],
            productListLookupData: [],
            customerGroupListLookupData: [],
            customerCategoryListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            orderDate: new Date(),
            description: '',
            customerId: null,
            taxId: null,
            orderStatus: null,
            errors: {
                orderDate: '',
                customerId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            },
            showComplexDiv: false,
            isSubmitting: false,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00',
            amountInWords: '',
            customerQuickName: '',
            customerQuickDescription: '',
            customerQuickStreet: '',
            customerQuickCity: '',
            customerQuickAddrState: '',
            customerQuickZipCode: '',
            customerQuickCountry: '',
            customerQuickPhoneNumber: '',
            customerQuickEmailAddress: '',
            customerQuickGroupId: null,
            customerQuickCategoryId: null,
            customerQuickIsSubmitting: false,
            customerQuickErrors: {
                name: '', street: '', city: '', addrState: '', zipCode: '',
                phoneNumber: '', emailAddress: '', customerGroupId: '', customerCategoryId: ''
            },
            customerGroupQuickName: '',
            customerGroupQuickDescription: '',
            customerGroupQuickIsSubmitting: false,
            customerGroupQuickErrors: { name: '' },
            customerCategoryQuickName: '',
            customerCategoryQuickDescription: '',
            customerCategoryQuickIsSubmitting: false,
            customerCategoryQuickErrors: { name: '' },
            barcodeInput: ''
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const customerIdRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const customerQuickModalRef = Vue.ref(null);
        const customerGroupQuickModalRef = Vue.ref(null);
        const customerCategoryQuickModalRef = Vue.ref(null);
        const customerQuickGroupIdRef = Vue.ref(null);
        const customerQuickCategoryIdRef = Vue.ref(null);
        const barcodeScanRef = Vue.ref(null);

        // Tracks available stock for the product being edited in the line-item grid
        let currentEditAvailableStock = Infinity;
        let currentEditProductPhysical = false;
        let currentEditProductName = '';

        const validateForm = function () {
            state.errors.orderDate = '';
            state.errors.customerId = '';
            state.errors.taxId = '';
            state.errors.orderStatus = '';

            let isValid = true;

            if (!state.orderDate) {
                state.errors.orderDate = 'Order date is required.';
                isValid = false;
            }
            if (!state.customerId) {
                state.errors.customerId = 'Customer is required.';
                isValid = false;
            }
            if (!state.taxId) {
                state.errors.taxId = 'Tax is required.';
                isValid = false;
            }
            if (!state.orderStatus) {
                state.errors.orderStatus = 'Order status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.orderDate = new Date();
            state.description = '';
            state.customerId = null;
            state.taxId = null;
            state.orderStatus = null;
            state.errors = {
                orderDate: '',
                customerId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            };
            state.secondaryData = [];
            state.subTotalAmount = '0.00';
            state.taxAmount = '0.00';
            state.totalAmount = '0.00';
            state.amountInWords = '';
            state.showComplexDiv = false;
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (orderDate, description, orderStatus, taxId, customerId, createdById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/CreateSalesOrder', {
                        orderDate, description, orderStatus, taxId, customerId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, orderDate, description, orderStatus, taxId, customerId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/UpdateSalesOrder', {
                        id, orderDate, description, orderStatus, taxId, customerId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/DeleteSalesOrder', {
                        id, deletedById
                    });
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
            getTaxListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Tax/GetTaxList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesOrderStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSecondaryData: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get('/SalesOrderItem/GetSalesOrderItemBySalesOrderIdList?salesOrderId=' + salesOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createSecondaryData: async (unitPrice, quantity, remark, productId, salesOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/CreateSalesOrderItem', {
                        unitPrice, quantity, remark, productId, salesOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, unitPrice, quantity, remark, productId, salesOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/UpdateSalesOrderItem', {
                        id, unitPrice, quantity, remark, productId, salesOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/DeleteSalesOrderItem', {
                        id, deletedById
                    });
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
            getInventoryStockList: async () => {
                try {
                    const response = await AxiosManager.get('/InventoryTransaction/GetInventoryStockList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createCustomer: async (name, description, customerGroupId, customerCategoryId, street, city, addressState, zipCode, country, phoneNumber, emailAddress, createdById) => {
                try {
                    const response = await AxiosManager.post('/Customer/CreateCustomer', {
                        name, description, customerGroupId, customerCategoryId,
                        street, city, state: addressState, zipCode, country,
                        phoneNumber, emailAddress, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerGroupListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/CustomerGroup/GetCustomerGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createCustomerGroup: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/CustomerGroup/CreateCustomerGroup', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerCategoryListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/CustomerCategory/GetCustomerCategoryList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createCustomerCategory: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/CustomerCategory/CreateCustomerCategory', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductByBarcode: async (barcode) => {
                try {
                    const response = await AxiosManager.get('/Product/GetProductByBarcode', { params: { barcode } });
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populateCustomerListLookupData: async () => {
                const response = await services.getCustomerListLookupData();
                state.customerListLookupData = response?.data?.content?.data;
            },
            populateTaxListLookupData: async () => {
                const response = await services.getTaxListLookupData();
                state.taxListLookupData = response?.data?.content?.data;
            },
            populateSalesOrderStatusListLookupData: async () => {
                const response = await services.getSalesOrderStatusListLookupData();
                state.salesOrderStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSecondaryData: async (salesOrderId) => {
                try {
                    const response = await services.getSecondaryData(salesOrderId);
                    state.secondaryData = response?.data?.content?.data.map(item => ({
                        ...item,
                        createdAtUtc: new Date(item.createdAtUtc)
                    }));
                    methods.refreshPaymentSummary(salesOrderId);
                } catch (error) {
                    state.secondaryData = [];
                }
            },
            populateProductListLookupData: async () => {
                const [productResponse, stockResponse] = await Promise.all([
                    services.getProductListLookupData(),
                    services.getInventoryStockList()
                ]);

                const products = productResponse?.data?.content?.data ?? [];
                const stockList = stockResponse?.data?.content?.data ?? [];

                const stockByProduct = {};
                stockList.forEach(s => {
                    const pid = s.productId;
                    stockByProduct[pid] = (stockByProduct[pid] ?? 0) + (s.stock ?? 0);
                });

                state.productListLookupData = products.map(p => ({
                    ...p,
                    availableStock: stockByProduct[p.id] ?? 0
                }));
            },
            populateCustomerGroupListLookupData: async () => {
                const response = await services.getCustomerGroupListLookupData();
                state.customerGroupListLookupData = response?.data?.content?.data;
            },
            populateCustomerCategoryListLookupData: async () => {
                const response = await services.getCustomerCategoryListLookupData();
                state.customerCategoryListLookupData = response?.data?.content?.data;
            },
            refreshPaymentSummary: async (id) => {
                const record = state.mainData.find(item => item.id === id);
                if (record) {
                    state.subTotalAmount = NumberFormatManager.formatToLocale(record.beforeTaxAmount ?? 0);
                    state.taxAmount = NumberFormatManager.formatToLocale(record.taxAmount ?? 0);
                    state.totalAmount = NumberFormatManager.formatToLocale(record.afterTaxAmount ?? 0);
                    state.amountInWords = AmountInWordsManager.convert(record.afterTaxAmount ?? 0);
                }
            },
            handleFormSubmit: async () => {
                state.isSubmitting = true;
                await new Promise(resolve => setTimeout(resolve, 200));

                if (!validateForm()) {
                    state.isSubmitting = false;
                    return;
                }

                try {
                    const response = state.id === ''
                        ? await services.createMainData(state.orderDate, state.description, state.orderStatus, state.taxId, state.customerId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.orderDate, state.description, state.orderStatus, state.taxId, state.customerId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Sales Order';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.orderDate = response?.data?.content?.data.orderDate ? new Date(response.data.content.data.orderDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.customerId = response?.data?.content?.data.customerId ?? '';
                            state.taxId = response?.data?.content?.data.taxId ?? '';
                            taxListLookup.trackingChange = true;
                            state.orderStatus = String(response?.data?.content?.data.orderStatus ?? '');
                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        }

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode ? 'Delete Failed' : 'Save Failed',
                            text: response.data.message ?? 'Please check your data.',
                            confirmButtonText: 'Try Again'
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'An Error Occurred',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            onMainModalHidden: () => {
                state.errors.orderDate = '';
                state.errors.customerId = '';
                state.errors.taxId = '';
                state.errors.orderStatus = '';
                taxListLookup.trackingChange = false;
            }
        };

        const customerListLookup = {
            obj: null,
            create: () => {
                if (state.customerListLookupData && Array.isArray(state.customerListLookupData)) {
                    customerListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.customerListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select a Customer',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('name', 'startsWith', e.text, true);
                            }
                            e.updateData(state.customerListLookupData, query);
                        },
                        change: (e) => {
                            state.customerId = e.value;
                        }
                    });
                    customerListLookup.obj.appendTo(customerIdRef.value);
                }
            },
            refresh: () => {
                if (customerListLookup.obj) {
                    customerListLookup.obj.value = state.customerId;
                }
            }
        };

        const taxListLookup = {
            obj: null,
            trackingChange: false,
            create: () => {
                if (state.taxListLookupData && Array.isArray(state.taxListLookupData)) {
                    taxListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.taxListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select a Tax',
                        change: async (e) => {
                            state.taxId = e.value;
                            if (e.isInteracted && taxListLookup.trackingChange) {
                                await methods.handleFormSubmit();
                            }
                        }
                    });
                    taxListLookup.obj.appendTo(taxIdRef.value);
                }
            },
            refresh: () => {
                if (taxListLookup.obj) {
                    taxListLookup.obj.value = state.taxId;
                }
            }
        };

        const salesOrderStatusListLookup = {
            obj: null,
            create: () => {
                if (state.salesOrderStatusListLookupData && Array.isArray(state.salesOrderStatusListLookupData)) {
                    salesOrderStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.salesOrderStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select an Order Status',
                        change: (e) => {
                            state.orderStatus = e.value;
                        }
                    });
                    salesOrderStatusListLookup.obj.appendTo(orderStatusRef.value);
                }
            },
            refresh: () => {
                if (salesOrderStatusListLookup.obj) {
                    salesOrderStatusListLookup.obj.value = state.orderStatus;
                }
            }
        };

        const customerQuickGroupListLookup = {
            obj: null,
            create: () => {
                customerQuickGroupListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.customerGroupListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Customer Group',
                    popupHeight: '200px',
                    allowFiltering: true,
                    change: (e) => { state.customerQuickGroupId = e.value; }
                });
                customerQuickGroupListLookup.obj.appendTo(customerQuickGroupIdRef.value);
            },
            refresh: () => {
                if (customerQuickGroupListLookup.obj) {
                    customerQuickGroupListLookup.obj.setProperties({
                        dataSource: state.customerGroupListLookupData,
                        value: state.customerQuickGroupId
                    });
                }
            }
        };

        const customerQuickCategoryListLookup = {
            obj: null,
            create: () => {
                customerQuickCategoryListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.customerCategoryListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Customer Category',
                    popupHeight: '200px',
                    allowFiltering: true,
                    change: (e) => { state.customerQuickCategoryId = e.value; }
                });
                customerQuickCategoryListLookup.obj.appendTo(customerQuickCategoryIdRef.value);
            },
            refresh: () => {
                if (customerQuickCategoryListLookup.obj) {
                    customerQuickCategoryListLookup.obj.setProperties({
                        dataSource: state.customerCategoryListLookupData,
                        value: state.customerQuickCategoryId
                    });
                }
            }
        };

        const customerQuickModal = {
            obj: null,
            create: () => {
                customerQuickModal.obj = new bootstrap.Modal(customerQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const customerGroupQuickModal = {
            obj: null,
            create: () => {
                customerGroupQuickModal.obj = new bootstrap.Modal(customerGroupQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const customerCategoryQuickModal = {
            obj: null,
            create: () => {
                customerCategoryQuickModal.obj = new bootstrap.Modal(customerCategoryQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const orderDatePicker = {
            obj: null,
            create: () => {
                orderDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.orderDate ? new Date(state.orderDate) : null,
                    change: (e) => {
                        state.orderDate = e.value;
                    }
                });
                orderDatePicker.obj.appendTo(orderDateRef.value);
            },
            refresh: () => {
                if (orderDatePicker.obj) {
                    orderDatePicker.obj.value = state.orderDate ? new Date(state.orderDate) : null;
                }
            }
        };

        const numberText = {
            obj: null,
            create: () => {
                numberText.obj = new ej.inputs.TextBox({
                    placeholder: '[auto]',
                    readonly: true
                });
                numberText.obj.appendTo(numberRef.value);
            }
        };

        Vue.watch(
            () => state.orderDate,
            (newVal, oldVal) => {
                orderDatePicker.refresh();
                state.errors.orderDate = '';
            }
        );

        Vue.watch(
            () => state.customerId,
            (newVal, oldVal) => {
                customerListLookup.refresh();
                state.errors.customerId = '';
            }
        );

        Vue.watch(
            () => state.taxId,
            (newVal, oldVal) => {
                taxListLookup.refresh();
                state.errors.taxId = '';
            }
        );

        Vue.watch(
            () => state.orderStatus,
            (newVal, oldVal) => {
                salesOrderStatusListLookup.refresh();
                state.errors.orderStatus = '';
            }
        );

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
                    groupSettings: {},
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'number', headerText: 'Number', width: 150, minWidth: 150 },
                        { field: 'orderDate', headerText: 'SO Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'customerName', headerText: 'Customer', width: 200, minWidth: 200 },
                        { field: 'orderStatusName', headerText: 'Status', width: 150, minWidth: 150 },
                        { field: 'taxName', headerText: 'Tax', width: 150, minWidth: 150 },
                        { field: 'afterTaxAmount', headerText: 'Total Amount', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                        { text: 'Print PDF', tooltipText: 'Print PDF', id: 'PrintPDFCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                    },
                    beforeExcelExport: (args) => {
                        args.data = args.data.map(function (row) {
                            return Object.assign({}, row, {
                                orderDate: row.orderDate instanceof Date
                                    ? row.orderDate.toLocaleDateString('en-CA')
                                    : row.orderDate,
                                createdAtUtc: row.createdAtUtc instanceof Date
                                    ? row.createdAtUtc.toLocaleDateString('en-CA') + ' ' + row.createdAtUtc.toTimeString().slice(0, 5)
                                    : row.createdAtUtc
                            });
                        });
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            const date = new Date().toISOString().slice(0, 10);
                            mainGrid.obj.excelExport({ fileName: `SalesOrders_${date}.xlsx` });
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Sales Order';
                            resetFormState();
                            state.secondaryData = [];
                            secondaryGrid.refresh();
                            state.showComplexDiv = false;
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Sales Order';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.customerId = selectedRecord.customerId ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                taxListLookup.trackingChange = true;
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');
                                state.showComplexDiv = true;

                                await methods.populateSecondaryData(selectedRecord.id);
                                secondaryGrid.refresh();

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Sales Order?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.customerId = selectedRecord.customerId ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');
                                state.showComplexDiv = false;

                                await methods.populateSecondaryData(selectedRecord.id);
                                secondaryGrid.refresh();

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/SalesOrders/SalesOrderPdf?id=' + (selectedRecord.id ?? ''), '_blank');
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

        const secondaryGrid = {
            obj: null,
            create: async (dataSource) => {
                secondaryGrid.obj = new ej.grids.Grid({
                    height: 400,
                    dataSource: dataSource,
                    editSettings: { allowEditing: true, allowAdding: true, allowDeleting: true, showDeleteConfirmDialog: true, mode: 'Normal', allowEditOnDblClick: true },
                    allowFiltering: false,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: false,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'productName', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: false,
                    showColumnMenu: false,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        {
                            field: 'productId',
                            headerText: 'Product',
                            width: 250,
                            validationRules: { required: true },
                            valueAccessor: (field, data, column) => {
                                const product = state.productListLookupData.find(item => item.id === data[field]);
                                return product ? `${product.name}` : '';
                            },
                            editType: 'dropdownedit',
                            edit: {
                                create: () => {
                                    let productElem = document.createElement('input');
                                    return productElem;
                                },
                                read: () => {
                                    return productObj.value;
                                },
                                destroy: () => {
                                    productObj.destroy();
                                },
                                write: (args) => {
                                    const initProduct = state.productListLookupData.find(p => p.id === args.rowData.productId);
                                    if (initProduct) {
                                        currentEditAvailableStock = initProduct.physical ? (initProduct.availableStock ?? 0) : Infinity;
                                        currentEditProductPhysical = initProduct.physical ?? false;
                                        currentEditProductName = initProduct.name;
                                    }

                                    productObj = new ej.dropdowns.DropDownList({
                                        dataSource: state.productListLookupData,
                                        fields: { value: 'id', text: 'name' },
                                        value: args.rowData.productId,
                                        change: (e) => {
                                            const selectedProduct = state.productListLookupData.find(item => item.id === e.value);
                                            if (selectedProduct) {
                                                currentEditAvailableStock = selectedProduct.physical ? (selectedProduct.availableStock ?? 0) : Infinity;
                                                currentEditProductPhysical = selectedProduct.physical ?? false;
                                                currentEditProductName = selectedProduct.name;

                                                args.rowData.productId = selectedProduct.id;
                                                if (numberObj) {
                                                    numberObj.value = selectedProduct.number;
                                                }
                                                if (priceObj) {
                                                    priceObj.value = selectedProduct.unitPrice;
                                                }
                                                if (remarkObj) {
                                                    remarkObj.value = selectedProduct.description;
                                                }
                                                if (quantityObj) {
                                                    quantityObj.value = 1;
                                                    quantityObj.placeholder = selectedProduct.physical
                                                        ? `Max available: ${selectedProduct.availableStock ?? 0}`
                                                        : '';
                                                    const total = selectedProduct.unitPrice * quantityObj.value;
                                                    if (totalObj) {
                                                        totalObj.value = total;
                                                    }
                                                }
                                            }
                                        },
                                        placeholder: 'Select a Product',
                                        floatLabelType: 'Never'
                                    });
                                    productObj.appendTo(args.element);
                                }
                            }
                        },
                        {
                            field: 'unitPrice',
                            headerText: 'Unit Price',
                            width: 200, validationRules: { required: true }, type: 'number', format: 'N2', textAlign: 'Right',
                            edit: {
                                create: () => {
                                    let priceElem = document.createElement('input');
                                    return priceElem;
                                },
                                read: () => {
                                    return priceObj.value;
                                },
                                destroy: () => {
                                    priceObj.destroy();
                                },
                                write: (args) => {
                                    priceObj = new ej.inputs.NumericTextBox({
                                        value: args.rowData.unitPrice ?? 0,
                                        change: (e) => {
                                            if (quantityObj && totalObj) {
                                                const total = e.value * quantityObj.value;
                                                totalObj.value = total;
                                            }
                                        }
                                    });
                                    priceObj.appendTo(args.element);
                                }
                            }
                        },
                        {
                            field: 'quantity',
                            headerText: 'Quantity',
                            width: 200,
                            validationRules: {
                                required: true,
                                custom: [(args) => {
                                    return args['value'] > 0;
                                }, 'Must be a positive number and not zero']
                            },
                            type: 'number', format: 'N2', textAlign: 'Right',
                            edit: {
                                create: () => {
                                    let quantityElem = document.createElement('input');
                                    return quantityElem;
                                },
                                read: () => {
                                    return quantityObj.value;
                                },
                                destroy: () => {
                                    quantityObj.destroy();
                                },
                                write: (args) => {
                                    const product = state.productListLookupData.find(p => p.id === args.rowData.productId);
                                    const stockHint = product?.physical ? `Max: ${product.availableStock ?? 0}` : '';

                                    quantityObj = new ej.inputs.NumericTextBox({
                                        value: args.rowData.quantity ?? 0,
                                        placeholder: stockHint,
                                        change: (e) => {
                                            if (priceObj && totalObj) {
                                                const total = e.value * priceObj.value;
                                                totalObj.value = total;
                                            }
                                        }
                                    });
                                    quantityObj.appendTo(args.element);
                                }
                            }
                        },
                        {
                            field: 'total',
                            headerText: 'Total',
                            width: 200, validationRules: { required: false }, type: 'number', format: 'N2', textAlign: 'Right',
                            edit: {
                                create: () => {
                                    let totalElem = document.createElement('input');
                                    return totalElem;
                                },
                                read: () => {
                                    return totalObj.value;
                                },
                                destroy: () => {
                                    totalObj.destroy();
                                },
                                write: (args) => {
                                    totalObj = new ej.inputs.NumericTextBox({
                                        value: args.rowData.total ?? 0,
                                        readonly: true
                                    });
                                    totalObj.appendTo(args.element);
                                }
                            }
                        },
                        {
                            field: 'productNumber',
                            headerText: 'Product Number',
                            allowEditing: false,
                            width: 180,
                            edit: {
                                create: () => {
                                    let numberElem = document.createElement('input');
                                    return numberElem;
                                },
                                read: () => {
                                    return numberObj.value;
                                },
                                destroy: () => {
                                    numberObj.destroy();
                                },
                                write: (args) => {
                                    numberObj = new ej.inputs.TextBox();
                                    numberObj.value = args.rowData.productNumber;
                                    numberObj.readonly = true;
                                    numberObj.appendTo(args.element);
                                }
                            }
                        },
                        {
                            field: 'availableStock',
                            headerText: 'Available Stock',
                            width: 160,
                            allowEditing: false,
                            type: 'number',
                            format: 'N2',
                            textAlign: 'Right',
                            valueAccessor: (field, data) => {
                                if (!data.productId) return '—';
                                const product = state.productListLookupData.find(p => p.id === data.productId);
                                if (!product || !product.physical) return 'N/A';
                                return product.availableStock ?? 0;
                            }
                        },
                        {
                            field: 'remark',
                            headerText: 'Remark',
                            width: 200,
                            edit: {
                                create: () => {
                                    let remarkElem = document.createElement('input');
                                    return remarkElem;
                                },
                                read: () => {
                                    return remarkObj.value;
                                },
                                destroy: () => {
                                    remarkObj.destroy();
                                },
                                write: (args) => {
                                    remarkObj = new ej.inputs.TextBox();
                                    remarkObj.value = args.rowData.remark;
                                    remarkObj.appendTo(args.element);
                                }
                            }
                        },
                    ],
                    toolbar: [
                        'ExcelExport',
                        { type: 'Separator' },
                        'Add', 'Edit', 'Delete', 'Update', 'Cancel',
                    ],
                    actionBegin: (args) => {
                        if (args.requestType === 'save') {
                            const data = args.data;
                            const product = state.productListLookupData.find(p => p.id === data.productId);

                            if (product && product.physical) {
                                const available = product.availableStock ?? 0;
                                const requested = data.quantity ?? 0;

                                if (requested > available) {
                                    args.cancel = true;
                                    Swal.fire({
                                        icon: 'error',
                                        title: 'Insufficient Stock',
                                        html: `<b>${product.name}</b><br>` +
                                              `Available Stock: <b>${available.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</b><br>` +
                                              `Requested Quantity: <b>${requested.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</b><br><br>` +
                                              `Cannot save. Requested quantity exceeds available stock.`,
                                        confirmButtonText: 'OK'
                                    });
                                }
                            }
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () { },
                    beforeExcelExport: (args) => {
                        args.data = args.data.map(function (row) {
                            var cloned = Object.assign({}, row);
                            if (cloned.createdAtUtc instanceof Date) {
                                cloned.createdAtUtc = cloned.createdAtUtc.toLocaleDateString('en-CA') + ' ' + cloned.createdAtUtc.toTimeString().slice(0, 5);
                            }
                            return cloned;
                        });
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length) {
                            secondaryGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'SecondaryGrid_excelexport') {
                            const date = new Date().toISOString().slice(0, 10);
                            secondaryGrid.obj.excelExport({ fileName: `SalesOrderItems_${date}.xlsx` });
                        }
                    },
                    actionComplete: async (args) => {
                        if (args.requestType === 'save' && args.action === 'add') {
                            const salesOrderId = state.id; 
                            const userId = StorageManager.getUserId();
                            const data = args.data;

                            await services.createSecondaryData(data?.unitPrice, data?.quantity, data?.remark, data?.productId, salesOrderId, userId);
                            await methods.populateSecondaryData(salesOrderId);
                            secondaryGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'save' && args.action === 'edit') {
                            const salesOrderId = state.id; 
                            const userId = StorageManager.getUserId();
                            const data = args.data;

                            await services.updateSecondaryData(data?.id, data?.unitPrice, data?.quantity, data?.remark, data?.productId, salesOrderId, userId);
                            await methods.populateSecondaryData(salesOrderId);
                            secondaryGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'delete') {
                            const salesOrderId = state.id; 
                            const userId = StorageManager.getUserId();
                            const data = args.data[0];

                            await services.deleteSecondaryData(data?.id, userId);
                            await methods.populateSecondaryData(salesOrderId);
                            secondaryGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }

                        await methods.populateMainData();
                        mainGrid.refresh();
                        await methods.refreshPaymentSummary(state.id);
                    }
                });
                secondaryGrid.obj.appendTo(secondaryGridRef.value);
            },
            refresh: () => {
                secondaryGrid.obj.setProperties({ dataSource: state.secondaryData });
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['SalesOrders']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                orderDatePicker.create();
                numberText.create();
                await secondaryGrid.create(state.secondaryData);

                Promise.all([
                    methods.populateCustomerListLookupData(),
                    methods.populateTaxListLookupData(),
                    methods.populateSalesOrderStatusListLookupData(),
                    methods.populateProductListLookupData(),
                    methods.populateCustomerGroupListLookupData(),
                    methods.populateCustomerCategoryListLookupData(),
                ]).then(() => {
                    customerListLookup.create();
                    taxListLookup.create();
                    salesOrderStatusListLookup.create();
                    customerQuickGroupListLookup.create();
                    customerQuickCategoryListLookup.create();
                    customerQuickModal.create();
                    customerGroupQuickModal.create();
                    customerCategoryQuickModal.create();
                });
            } catch (e) {
            } finally {

            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', methods.onMainModalHidden);
        });

        return {
            mainGridRef,
            mainModalRef,
            orderDateRef,
            numberRef,
            customerIdRef,
            taxIdRef,
            orderStatusRef,
            secondaryGridRef,
            customerQuickModalRef,
            customerGroupQuickModalRef,
            customerCategoryQuickModalRef,
            customerQuickGroupIdRef,
            customerQuickCategoryIdRef,
            barcodeScanRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit,
                openCustomerQuickCreate: () => {
                    state.customerQuickName = '';
                    state.customerQuickDescription = '';
                    state.customerQuickStreet = '';
                    state.customerQuickCity = '';
                    state.customerQuickAddrState = '';
                    state.customerQuickZipCode = '';
                    state.customerQuickCountry = '';
                    state.customerQuickPhoneNumber = '';
                    state.customerQuickEmailAddress = '';
                    state.customerQuickGroupId = null;
                    state.customerQuickCategoryId = null;
                    state.customerQuickErrors = {
                        name: '', street: '', city: '', addrState: '', zipCode: '',
                        phoneNumber: '', emailAddress: '', customerGroupId: '', customerCategoryId: ''
                    };
                    customerQuickGroupListLookup.refresh();
                    customerQuickCategoryListLookup.refresh();
                    customerQuickModal.obj.show();
                },
                closeCustomerQuickCreate: () => {
                    customerQuickModal.obj.hide();
                },
                submitCustomerQuickCreate: async () => {
                    state.customerQuickErrors = {
                        name: '', street: '', city: '', addrState: '', zipCode: '',
                        phoneNumber: '', emailAddress: '', customerGroupId: '', customerCategoryId: ''
                    };
                    let isValid = true;
                    if (!state.customerQuickName?.trim()) { state.customerQuickErrors.name = 'Name is required.'; isValid = false; }
                    if (!state.customerQuickStreet?.trim()) { state.customerQuickErrors.street = 'Street is required.'; isValid = false; }
                    if (!state.customerQuickCity?.trim()) { state.customerQuickErrors.city = 'City is required.'; isValid = false; }
                    if (!state.customerQuickAddrState?.trim()) { state.customerQuickErrors.addrState = 'State / Province is required.'; isValid = false; }
                    if (!state.customerQuickZipCode?.trim()) { state.customerQuickErrors.zipCode = 'Zip code is required.'; isValid = false; }
                    if (!state.customerQuickPhoneNumber?.trim()) { state.customerQuickErrors.phoneNumber = 'Phone number is required.'; isValid = false; }
                    if (!state.customerQuickEmailAddress?.trim()) { state.customerQuickErrors.emailAddress = 'Email address is required.'; isValid = false; }
                    if (!state.customerQuickGroupId) { state.customerQuickErrors.customerGroupId = 'Customer group is required.'; isValid = false; }
                    if (!state.customerQuickCategoryId) { state.customerQuickErrors.customerCategoryId = 'Customer category is required.'; isValid = false; }
                    if (!isValid) return;
                    try {
                        state.customerQuickIsSubmitting = true;
                        const response = await services.createCustomer(
                            state.customerQuickName.trim(),
                            state.customerQuickDescription,
                            state.customerQuickGroupId,
                            state.customerQuickCategoryId,
                            state.customerQuickStreet.trim(),
                            state.customerQuickCity.trim(),
                            state.customerQuickAddrState.trim(),
                            state.customerQuickZipCode.trim(),
                            state.customerQuickCountry,
                            state.customerQuickPhoneNumber.trim(),
                            state.customerQuickEmailAddress.trim(),
                            StorageManager.getUserId()
                        );
                        if (response.data.code === 200) {
                            const newCustomer = response.data.content.data;
                            await methods.populateCustomerListLookupData();
                            customerListLookup.obj.setProperties({ dataSource: state.customerListLookupData, value: newCustomer.id });
                            state.customerId = newCustomer.id;
                            customerQuickModal.obj.hide();
                            Swal.fire({ icon: 'success', title: 'Customer Created', timer: 1500, showConfirmButton: false });
                        } else {
                            Swal.fire({ icon: 'error', title: 'Failed', text: response.data.message ?? 'Failed to create customer.' });
                        }
                    } catch (error) {
                        Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'An error occurred.' });
                    } finally {
                        state.customerQuickIsSubmitting = false;
                    }
                },
                openCustomerGroupQuickCreate: () => {
                    state.customerGroupQuickName = '';
                    state.customerGroupQuickDescription = '';
                    state.customerGroupQuickErrors = { name: '' };
                    customerQuickModal.obj.hide();
                    setTimeout(() => customerGroupQuickModal.obj.show(), 300);
                },
                closeCustomerGroupQuickCreate: () => {
                    customerGroupQuickModal.obj.hide();
                    setTimeout(() => customerQuickModal.obj.show(), 300);
                },
                submitCustomerGroupQuickCreate: async () => {
                    state.customerGroupQuickErrors = { name: '' };
                    if (!state.customerGroupQuickName?.trim()) {
                        state.customerGroupQuickErrors.name = 'Name is required.';
                        return;
                    }
                    try {
                        state.customerGroupQuickIsSubmitting = true;
                        const response = await services.createCustomerGroup(
                            state.customerGroupQuickName.trim(),
                            state.customerGroupQuickDescription,
                            StorageManager.getUserId()
                        );
                        if (response.data.code === 200) {
                            const newGroup = response.data.content.data;
                            await methods.populateCustomerGroupListLookupData();
                            customerQuickGroupListLookup.obj.setProperties({ dataSource: state.customerGroupListLookupData, value: newGroup.id });
                            state.customerQuickGroupId = newGroup.id;
                            customerGroupQuickModal.obj.hide();
                            setTimeout(() => customerQuickModal.obj.show(), 300);
                        } else {
                            state.customerGroupQuickErrors.name = response.data.message ?? 'Failed to create customer group.';
                        }
                    } catch (error) {
                        state.customerGroupQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                    } finally {
                        state.customerGroupQuickIsSubmitting = false;
                    }
                },
                openCustomerCategoryQuickCreate: () => {
                    state.customerCategoryQuickName = '';
                    state.customerCategoryQuickDescription = '';
                    state.customerCategoryQuickErrors = { name: '' };
                    customerQuickModal.obj.hide();
                    setTimeout(() => customerCategoryQuickModal.obj.show(), 300);
                },
                closeCustomerCategoryQuickCreate: () => {
                    customerCategoryQuickModal.obj.hide();
                    setTimeout(() => customerQuickModal.obj.show(), 300);
                },
                submitCustomerCategoryQuickCreate: async () => {
                    state.customerCategoryQuickErrors = { name: '' };
                    if (!state.customerCategoryQuickName?.trim()) {
                        state.customerCategoryQuickErrors.name = 'Name is required.';
                        return;
                    }
                    try {
                        state.customerCategoryQuickIsSubmitting = true;
                        const response = await services.createCustomerCategory(
                            state.customerCategoryQuickName.trim(),
                            state.customerCategoryQuickDescription,
                            StorageManager.getUserId()
                        );
                        if (response.data.code === 200) {
                            const newCategory = response.data.content.data;
                            await methods.populateCustomerCategoryListLookupData();
                            customerQuickCategoryListLookup.obj.setProperties({ dataSource: state.customerCategoryListLookupData, value: newCategory.id });
                            state.customerQuickCategoryId = newCategory.id;
                            customerCategoryQuickModal.obj.hide();
                            setTimeout(() => customerQuickModal.obj.show(), 300);
                        } else {
                            state.customerCategoryQuickErrors.name = response.data.message ?? 'Failed to create customer category.';
                        }
                    } catch (error) {
                        state.customerCategoryQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                    } finally {
                        state.customerCategoryQuickIsSubmitting = false;
                    }
                },
                handleBarcodeInput: async () => {
                    const barcode = (state.barcodeInput ?? '').trim();
                    if (!barcode) return;

                    if (!state.id) {
                        Swal.fire({ icon: 'warning', title: 'Save the order first', text: 'Please save the Sales Order header before adding items via barcode.' });
                        return;
                    }

                    try {
                        const response = await services.getProductByBarcode(barcode);
                        const product = response?.data?.content?.data;

                        if (!product) {
                            Swal.fire({ icon: 'error', title: 'Product Not Found', text: `No product found for barcode: ${barcode}` });
                            state.barcodeInput = '';
                            return;
                        }

                        const matchedProduct = state.productListLookupData.find(p => p.id === product.id);
                        const unitPrice = matchedProduct?.unitPrice ?? product.unitPrice ?? 0;
                        const quantity = 1;

                        if (product.physical) {
                            const available = matchedProduct?.availableStock ?? 0;
                            if (quantity > available) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Insufficient Stock',
                                    html: `<b>${product.name}</b><br>Available: <b>${available}</b>`
                                });
                                state.barcodeInput = '';
                                return;
                            }
                        }

                        await services.createSecondaryData(unitPrice, quantity, null, product.id, state.id, StorageManager.getUserId());
                        await methods.populateSecondaryData(state.id);
                        secondaryGrid.refresh();
                        await methods.populateMainData();
                        mainGrid.refresh();
                        await methods.refreshPaymentSummary(state.id);

                        Swal.fire({ icon: 'success', title: `Added: ${product.name}`, timer: 1200, showConfirmButton: false });
                    } catch (error) {
                        Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Failed to add product by barcode.' });
                    } finally {
                        state.barcodeInput = '';
                        if (barcodeScanRef.value) barcodeScanRef.value.focus();
                    }
                }
            }
        };
    }
};

Vue.createApp(App).mount('#app');