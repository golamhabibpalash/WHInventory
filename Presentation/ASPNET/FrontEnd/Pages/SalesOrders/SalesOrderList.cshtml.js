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
            isSubmitting: false,
            isAddingLine: false,
            productPick: {
                productId: null,
                unitPrice: 0,
                quantity: 1
            },
            productHint: {
                name: '',
                physical: false,
                stock: null,
                minPrice: null,
                maxPrice: null
            },
            paymentMethodListLookupData: [],
            paymentList: [],
            paymentSummary: null,
            isFullySettled: false,
            isPaymentSubmitting: false,
            paymentError: '',
            newPayment: {
                paymentDate: '',
                paymentMethodId: '',
                amount: null,
                referenceNumber: ''
            },
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
            taxQuickName: '',
            taxQuickPercentage: null,
            taxQuickDescription: '',
            taxQuickIsSubmitting: false,
            taxQuickErrors: { name: '', percentage: '' },
            barcodeInput: ''
        });

        const mainGridRef = Vue.ref(null);
        const paymentDateRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const customerIdRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const productPickRef = Vue.ref(null);
        const customerQuickModalRef = Vue.ref(null);
        const customerGroupQuickModalRef = Vue.ref(null);
        const customerCategoryQuickModalRef = Vue.ref(null);
        const taxQuickModalRef = Vue.ref(null);
        const customerQuickGroupIdRef = Vue.ref(null);
        const customerQuickCategoryIdRef = Vue.ref(null);
        const barcodeScanRef = Vue.ref(null);

        // Running line total for the "Select Product" form.
        const posLineTotal = Vue.computed(() => (state.productPick.unitPrice || 0) * (state.productPick.quantity || 0));

        // Warns when the picked quantity is above what the warehouse can supply. The server is the
        // authority; this only nudges the cashier before they hit "Add to Cart".
        const posStockExceeded = Vue.computed(() =>
            state.productHint.physical
            && state.productHint.stock !== null
            && (state.productPick.quantity || 0) > state.productHint.stock);

        // Warns when the unit price falls outside the product's allowed selling band
        // (see ValidateSellingPriceAsync on the server, which enforces it on save).
        const posPriceOutOfBand = Vue.computed(() => {
            const price = state.productPick.unitPrice;
            if (price === null || price === undefined || price === '') return false;
            const belowMin = state.productHint.minPrice !== null && price < state.productHint.minPrice;
            const aboveMax = state.productHint.maxPrice !== null && price > state.productHint.maxPrice;
            return belowMin || aboveMax;
        });

        const formatQty = (value) => Number(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });

        // Quantity of a product already sitting in the cart, so stock checks account for it.
        const quantityInCart = (productId, exceptLineId) => state.secondaryData
            .filter(line => line.productId === productId && line.id !== exceptLineId)
            .reduce((sum, line) => sum + (line.quantity || 0), 0);

        const resetProductPick = () => {
            state.productPick = { productId: null, unitPrice: 0, quantity: 1 };
            state.productHint = { name: '', physical: false, stock: null, minPrice: null, maxPrice: null };
            if (productPickLookup.obj) {
                productPickLookup.obj.value = null;
                productPickLookup.obj.text = '';
            }
        };

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

        const paymentDatePicker = {
            obj: null,
            // The payment form is behind a v-if, so its host element comes and goes with the
            // modal. Rebuild the picker each time Vue hands back a fresh node.
            mount: (element) => {
                if (paymentDatePicker.obj) {
                    paymentDatePicker.obj.destroy();
                    paymentDatePicker.obj = null;
                }
                if (!element) return;

                paymentDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'Select date',
                    cssClass: 'e-small',
                    value: DateFormatManager.fromApiDate(state.newPayment.paymentDate),
                    change: (e) => {
                        state.newPayment.paymentDate = DateFormatManager.toApiDate(e.value) ?? '';
                    }
                });
                paymentDatePicker.obj.appendTo(element);
            },
            refresh: () => {
                if (paymentDatePicker.obj) {
                    paymentDatePicker.obj.value = DateFormatManager.fromApiDate(state.newPayment.paymentDate);
                }
            }
        };

        Vue.watch(() => paymentDateRef.value, (element) => paymentDatePicker.mount(element));

        const resetNewPaymentState = () => {
            state.newPayment = {
                paymentDate: DateFormatManager.toApiDate(new Date()),
                paymentMethodId: '',
                amount: null,
                referenceNumber: ''
            };
            paymentDatePicker.refresh();
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
            state.paymentList = [];
            state.paymentSummary = null;
            state.isFullySettled = false;
            state.paymentError = '';
            resetNewPaymentState();
            state.subTotalAmount = '0.00';
            state.taxAmount = '0.00';
            state.totalAmount = '0.00';
            state.amountInWords = '';
            resetProductPick();
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
            getPaymentMethodListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentMethod/GetPaymentMethodList?activeOnly=true', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentListByModule: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentListByModule?moduleName=SalesOrder&moduleId=${salesOrderId}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentSummaryData: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentSummary?moduleName=SalesOrder&moduleId=${salesOrderId}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createPayment: async (payload) => {
                try {
                    const response = await AxiosManager.post('/Payment/CreatePayment', payload);
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deletePaymentData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Payment/DeletePayment', { id, deletedById });
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
            },
            createTax: async (name, percentage, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/Tax/CreateTax', { name, percentage, description, createdById });
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

                await methods.refreshPaymentLedger(id);
            },
            refreshPaymentLedger: async (id) => {
                if (!id) {
                    state.paymentList = [];
                    state.paymentSummary = null;
                    state.isFullySettled = false;
                    return;
                }

                try {
                    const [listResponse, summaryResponse] = await Promise.all([
                        services.getPaymentListByModule(id),
                        services.getPaymentSummaryData(id)
                    ]);

                    state.paymentList = listResponse?.data?.content?.data ?? [];
                    state.paymentSummary = summaryResponse?.data?.content?.data ?? null;
                    state.isFullySettled = state.paymentSummary?.isFullySettled ?? false;
                } catch (error) {
                    state.paymentList = [];
                    state.paymentSummary = null;
                    state.isFullySettled = false;
                }
            },
            populatePaymentMethodListLookupData: async () => {
                const response = await services.getPaymentMethodListLookupData();
                state.paymentMethodListLookupData = response?.data?.content?.data ?? [];
            },
            handleFormSubmit: async () => {
                state.isSubmitting = true;

                if (!validateForm()) {
                    state.isSubmitting = false;
                    return;
                }

                // Captured before the response overwrites state.id, so we can tell a freshly
                // created header (step 1 -> step 2) from an edit of an existing one.
                const isNewOrder = state.id === '';

                try {
                    const response = isNewOrder
                        ? await services.createMainData(state.orderDate, state.description, state.orderStatus, state.taxId, state.customerId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.orderDate, state.description, state.orderStatus, state.taxId, state.customerId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.orderDate = response?.data?.content?.data.orderDate ? new Date(response.data.content.data.orderDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.customerId = response?.data?.content?.data.customerId ?? '';
                            state.taxId = response?.data?.content?.data.taxId ?? '';
                            taxListLookup.trackingChange = true;
                            state.orderStatus = String(response?.data?.content?.data.orderStatus ?? '');

                            state.mainTitle = `Sales Order ${state.number}`;

                            if (isNewOrder) {
                                // POS flow: stay on the same screen with the order now created,
                                // so the cashier can go straight to adding products.
                                await methods.populateSecondaryData(state.id);
                                await methods.refreshPaymentSummary(state.id);

                                Swal.fire({
                                    icon: 'success',
                                    title: 'Sales Order Created',
                                    text: `${state.number} saved. Add products to build the cart.`,
                                    timer: 1000,
                                    showConfirmButton: false
                                });
                            } else {
                                // Header edit: persist without leaving the POS screen.
                                Swal.fire({
                                    icon: 'success',
                                    title: 'Order Details Saved',
                                    timer: 1000,
                                    showConfirmButton: false
                                });
                            }
                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
                                text: 'Form will be closed...',
                                timer: 1000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 1000);
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

        const taxQuickModal = {
            obj: null,
            create: () => {
                taxQuickModal.obj = new bootstrap.Modal(taxQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const orderDatePicker = {
            obj: null,
            create: () => {
                orderDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
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
            },
            focus: () => {
                if (!orderDatePicker.obj) return;
                orderDatePicker.obj.focusIn();
                orderDatePicker.obj.element?.select?.();
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
                        { field: 'orderDate', headerText: 'SO Date', width: 150, format: 'dd/MM/yyyy' },
                        { field: 'customerName', headerText: 'Customer', width: 200, minWidth: 200 },
                        { field: 'orderStatusName', headerText: 'Status', width: 150, minWidth: 150 },
                        { field: 'taxName', headerText: 'Tax', width: 150, minWidth: 150 },
                        { field: 'afterTaxAmount', headerText: 'Total Amount', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'dd/MM/yyyy HH:mm' }
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
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            state.paymentError = '';
                            resetNewPaymentState();
                            resetProductPick();
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = `Sales Order ${selectedRecord.number ?? ''}`;
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.customerId = selectedRecord.customerId ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                taxListLookup.trackingChange = true;
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');

                                await methods.populateSecondaryData(selectedRecord.id);

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            state.paymentError = '';
                            resetNewPaymentState();
                            resetProductPick();
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

                                await methods.populateSecondaryData(selectedRecord.id);

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
                GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        // "Select Product" search box in the POS panel. Mirrors customerListLookup: a Syncfusion
        // DropDownList over the same product+stock dataset the cart validation uses.
        const productPickLookup = {
            obj: null,
            create: () => {
                if (!Array.isArray(state.productListLookupData)) return;

                productPickLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Search a product to add',
                    filterBarPlaceholder: 'Search',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        let query = new ej.data.Query();
                        if (e.text !== '') {
                            query = query.where('name', 'contains', e.text, true);
                        }
                        e.updateData(state.productListLookupData, query);
                    },
                    change: (e) => {
                        const product = state.productListLookupData.find(item => item.id === e.value);
                        if (!product) {
                            state.productHint = { name: '', physical: false, stock: null, minPrice: null, maxPrice: null };
                            return;
                        }
                        state.productPick.productId = product.id;
                        state.productPick.unitPrice = product.unitPrice ?? 0;
                        state.productPick.quantity = 1;
                        state.productHint = {
                            name: product.name,
                            physical: product.physical ?? false,
                            stock: product.physical ? (product.availableStock ?? 0) : null,
                            minPrice: product.minSellingPrice ?? null,
                            maxPrice: product.maxSellingPrice ?? null
                        };
                    }
                });
                productPickLookup.obj.appendTo(productPickRef.value);
            }
        };

        // The POS screen adds line items against a saved header. If the cashier starts by picking
        // a product before saving, create the header on the fly from the order bar fields.
        const ensureHeaderSaved = async () => {
            if (state.id) return true;

            if (!validateForm()) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Complete the order details',
                    text: 'Customer, tax, status and order date are required before adding products.'
                });
                return false;
            }

            try {
                const response = await services.createMainData(
                    state.orderDate, state.description, state.orderStatus, state.taxId, state.customerId, StorageManager.getUserId());

                if (response.data.code === 200) {
                    const data = response.data.content.data;
                    state.id = data.id ?? '';
                    state.number = data.number ?? '';
                    state.mainTitle = `Sales Order ${state.number}`;
                    taxListLookup.trackingChange = true;
                    await methods.populateMainData();
                    mainGrid.refresh();
                    return true;
                }

                Swal.fire({ icon: 'error', title: 'Save Failed', text: response.data.message ?? 'Could not create the sales order.' });
                return false;
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.' });
                return false;
            }
        };

        // Refresh everything that depends on the line items after a cart change.
        const refreshAfterCartChange = async () => {
            await methods.populateSecondaryData(state.id);
            await methods.populateMainData();
            mainGrid.refresh();
            await methods.refreshPaymentSummary(state.id);
        };

        const persistLine = async (line, changes) => {
            try {
                const unitPrice = changes.unitPrice ?? line.unitPrice;
                const quantity = changes.quantity ?? line.quantity;
                await services.updateSecondaryData(
                    line.id, unitPrice, quantity, line.remark, line.productId, state.id, StorageManager.getUserId());
                await refreshAfterCartChange();
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'Update Failed', text: error.response?.data?.message ?? 'An error occurred.' });
                await methods.populateSecondaryData(state.id);
            }
        };

        const cart = {
            addLine: async () => {
                const product = state.productListLookupData.find(p => p.id === state.productPick.productId);
                if (!product) {
                    Swal.fire({ icon: 'warning', title: 'Select a product first' });
                    return;
                }

                const quantity = Number(state.productPick.quantity);
                const unitPrice = Number(state.productPick.unitPrice);

                if (!quantity || quantity <= 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a quantity greater than zero' });
                    return;
                }
                if (isNaN(unitPrice) || unitPrice < 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit price' });
                    return;
                }

                if (product.physical) {
                    const available = product.availableStock ?? 0;
                    const alreadyInCart = quantityInCart(product.id);
                    if (quantity + alreadyInCart > available) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Insufficient Stock',
                            html: `<b>${product.name}</b><br>` +
                                  `Available: <b>${formatQty(available)}</b><br>` +
                                  `Already in cart: <b>${formatQty(alreadyInCart)}</b><br>` +
                                  `Requested: <b>${formatQty(quantity)}</b>`
                        });
                        return;
                    }
                }

                try {
                    state.isAddingLine = true;

                    if (!(await ensureHeaderSaved())) return;

                    await services.createSecondaryData(unitPrice, quantity, null, product.id, state.id, StorageManager.getUserId());
                    await refreshAfterCartChange();

                    resetProductPick();
                    Swal.fire({ icon: 'success', title: `Added: ${product.name}`, timer: 1000, showConfirmButton: false });
                    if (productPickLookup.obj) productPickLookup.obj.focusIn();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Could not add item', text: error.response?.data?.message ?? 'An error occurred.' });
                } finally {
                    state.isAddingLine = false;
                }
            },
            stepLineQty: (line, delta) => {
                const next = Math.max(0, (Number(line.quantity) || 0) + delta);
                cart.commitLineQty(line, next);
            },
            commitLineQty: async (line, value) => {
                const quantity = Number(value);
                if (!quantity || quantity <= 0) {
                    Swal.fire({ icon: 'warning', title: 'Quantity must be greater than zero' });
                    await methods.populateSecondaryData(state.id);
                    return;
                }

                const product = state.productListLookupData.find(p => p.id === line.productId);
                if (product && product.physical) {
                    const available = product.availableStock ?? 0;
                    const otherInCart = quantityInCart(line.productId, line.id);
                    if (quantity + otherInCart > available) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Insufficient Stock',
                            html: `<b>${product.name}</b><br>Available: <b>${formatQty(available)}</b>`
                        });
                        await methods.populateSecondaryData(state.id);
                        return;
                    }
                }

                await persistLine(line, { quantity });
            },
            commitLinePrice: async (line, value) => {
                const unitPrice = Number(value);
                if (isNaN(unitPrice) || unitPrice < 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit price' });
                    await methods.populateSecondaryData(state.id);
                    return;
                }
                await persistLine(line, { unitPrice });
            },
            removeLine: async (line) => {
                const confirmed = await Swal.fire({
                    icon: 'warning',
                    title: 'Remove this item?',
                    text: line.productName,
                    showCancelButton: true,
                    confirmButtonText: 'Remove',
                    confirmButtonColor: '#dc3545'
                });
                if (!confirmed.isConfirmed) return;

                try {
                    await services.deleteSecondaryData(line.id, StorageManager.getUserId());
                    await refreshAfterCartChange();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Delete Failed', text: error.response?.data?.message ?? 'An error occurred.' });
                }
            },
            clearAll: async () => {
                if (!state.secondaryData.length) return;

                const confirmed = await Swal.fire({
                    icon: 'warning',
                    title: 'Clear all items?',
                    text: `This removes all ${state.secondaryData.length} line item(s) from this order.`,
                    showCancelButton: true,
                    confirmButtonText: 'Clear All',
                    confirmButtonColor: '#dc3545'
                });
                if (!confirmed.isConfirmed) return;

                try {
                    const userId = StorageManager.getUserId();
                    for (const line of [...state.secondaryData]) {
                        await services.deleteSecondaryData(line.id, userId);
                    }
                    await refreshAfterCartChange();
                    Swal.fire({ icon: 'success', title: 'Cart Cleared', timer: 1200, showConfirmButton: false });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Some items could not be removed.' });
                    await methods.populateSecondaryData(state.id);
                }
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
                mainModalRef.value.addEventListener('shown.bs.modal', () => {
                    if (!state.deleteMode) {
                        orderDatePicker.focus();
                    }
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

                Promise.all([
                    methods.populateCustomerListLookupData(),
                    methods.populateTaxListLookupData(),
                    methods.populateSalesOrderStatusListLookupData(),
                    methods.populateProductListLookupData(),
                    methods.populatePaymentMethodListLookupData(),
                    methods.populateCustomerGroupListLookupData(),
                    methods.populateCustomerCategoryListLookupData(),
                ]).then(() => {
                    customerListLookup.create();
                    taxListLookup.create();
                    salesOrderStatusListLookup.create();
                    productPickLookup.create();
                    customerQuickGroupListLookup.create();
                    customerQuickCategoryListLookup.create();
                    customerQuickModal.create();
                    customerGroupQuickModal.create();
                    customerCategoryQuickModal.create();
                    taxQuickModal.create();
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
            paymentDateRef,
            mainModalRef,
            orderDateRef,
            numberRef,
            customerIdRef,
            taxIdRef,
            orderStatusRef,
            productPickRef,
            posLineTotal,
            posStockExceeded,
            posPriceOutOfBand,
            customerQuickModalRef,
            customerGroupQuickModalRef,
            customerCategoryQuickModalRef,
            taxQuickModalRef,
            customerQuickGroupIdRef,
            customerQuickCategoryIdRef,
            barcodeScanRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit,
                formatAmount: (value) => NumberFormatManager.formatToLocale(value ?? 0),
                formatQty: formatQty,
                stepQty: (delta) => {
                    const next = Math.max(0, (Number(state.productPick.quantity) || 0) + delta);
                    state.productPick.quantity = Number(next.toFixed(4));
                },
                clearProductPick: resetProductPick,
                addLineToCart: cart.addLine,
                stepLineQty: cart.stepLineQty,
                commitLineQty: cart.commitLineQty,
                commitLinePrice: cart.commitLinePrice,
                removeLine: cart.removeLine,
                clearCart: cart.clearAll,
                formatPaymentDate: (value) => {
                    if (!value) return '';
                    const d = new Date(value);
                    return isNaN(d) ? '' : d.toISOString().slice(0, 10);
                },
                payFullBalance: () => {
                    state.paymentError = '';
                    const outstanding = state.paymentSummary?.amountOutstanding ?? 0;
                    state.newPayment.amount = outstanding > 0 ? Number(outstanding.toFixed(2)) : null;
                },
                submitPayment: async () => {
                    state.paymentError = '';

                    const amount = Number(state.newPayment.amount);

                    if (!state.newPayment.paymentDate) {
                        state.paymentError = 'Payment date is required.';
                        return;
                    }
                    if (!state.newPayment.paymentMethodId) {
                        state.paymentError = 'Select a payment method.';
                        return;
                    }
                    if (!amount || amount <= 0) {
                        state.paymentError = 'Enter an amount greater than zero.';
                        return;
                    }

                    const outstanding = state.paymentSummary?.amountOutstanding ?? 0;
                    if (amount > outstanding + 0.005) {
                        state.paymentError = `Amount exceeds the balance due of ${NumberFormatManager.formatToLocale(outstanding)}.`;
                        return;
                    }

                    try {
                        state.isPaymentSubmitting = true;

                        const response = await services.createPayment({
                            moduleName: 'SalesOrder',
                            moduleId: state.id,
                            paymentDate: state.newPayment.paymentDate,
                            amount: amount,
                            paymentMethodId: state.newPayment.paymentMethodId,
                            referenceNumber: state.newPayment.referenceNumber || null,
                            createdById: StorageManager.getUserId()
                        });

                        if (response.data.code === 200) {
                            resetNewPaymentState();
                            await methods.refreshPaymentLedger(state.id);

                            Swal.fire({
                                icon: 'success',
                                title: 'Payment Recorded',
                                text: `Balance due: ${NumberFormatManager.formatToLocale(state.paymentSummary?.amountOutstanding ?? 0)}`,
                                timer: 1600,
                                showConfirmButton: false
                            });
                        }
                    } catch (error) {
                        state.paymentError = error.response?.data?.message ?? 'Failed to record the payment.';
                    } finally {
                        state.isPaymentSubmitting = false;
                    }
                },
                deletePayment: async (payment) => {
                    const confirmed = await Swal.fire({
                        icon: 'warning',
                        title: 'Remove this payment?',
                        html: `<strong>${NumberFormatManager.formatToLocale(payment.amount ?? 0)}</strong> via ${payment.paymentMethodName || 'unknown method'}.<br>The balance due will go back up.`,
                        showCancelButton: true,
                        confirmButtonText: 'Remove',
                        confirmButtonColor: '#dc3545'
                    });

                    if (!confirmed.isConfirmed) return;

                    try {
                        state.isPaymentSubmitting = true;
                        await services.deletePaymentData(payment.id, StorageManager.getUserId());
                        await methods.refreshPaymentLedger(state.id);
                    } catch (error) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: error.response?.data?.message ?? 'Failed to remove the payment.'
                        });
                    } finally {
                        state.isPaymentSubmitting = false;
                    }
                },
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
                openTaxQuickCreate: () => {
                    state.taxQuickName = '';
                    state.taxQuickPercentage = null;
                    state.taxQuickDescription = '';
                    state.taxQuickErrors = { name: '', percentage: '' };
                    taxQuickModal.obj.show();
                },
                closeTaxQuickCreate: () => {
                    taxQuickModal.obj.hide();
                },
                submitTaxQuickCreate: async () => {
                    const errors = { name: '', percentage: '' };
                    let isValid = true;
                    if (!state.taxQuickName?.trim()) { errors.name = 'Name is required.'; isValid = false; }
                    if (state.taxQuickPercentage === null || state.taxQuickPercentage === '' || isNaN(Number(state.taxQuickPercentage))) {
                        errors.percentage = 'Percentage is required.';
                        isValid = false;
                    }
                    state.taxQuickErrors = errors;
                    if (!isValid) return;

                    try {
                        state.taxQuickIsSubmitting = true;
                        const response = await services.createTax(
                            state.taxQuickName.trim(),
                            Number(state.taxQuickPercentage),
                            state.taxQuickDescription,
                            StorageManager.getUserId()
                        );
                        if (response.data.code === 200) {
                            const newTax = response.data.content.data;
                            await methods.populateTaxListLookupData();
                            taxListLookup.obj.setProperties({ dataSource: state.taxListLookupData, value: newTax.id });
                            state.taxId = newTax.id;
                            taxQuickModal.obj.hide();
                            Swal.fire({
                                icon: 'success',
                                title: 'Tax Created',
                                text: `"${newTax.name}" has been created and selected.`,
                                timer: 1000,
                                showConfirmButton: false
                            });
                        } else {
                            Swal.fire({ icon: 'error', title: 'Create Failed', text: response.data.message ?? 'Please check your data.', confirmButtonText: 'Try Again' });
                        }
                    } catch (error) {
                        Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                    } finally {
                        state.taxQuickIsSubmitting = false;
                    }
                },
                handleBarcodeInput: async () => {
                    const barcode = (state.barcodeInput ?? '').trim();
                    if (!barcode) return;

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
                            const alreadyInCart = quantityInCart(product.id);
                            if (quantity + alreadyInCart > available) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Insufficient Stock',
                                    html: `<b>${product.name}</b><br>` +
                                          `Available: <b>${formatQty(available)}</b><br>` +
                                          `Already in cart: <b>${formatQty(alreadyInCart)}</b>`
                                });
                                state.barcodeInput = '';
                                return;
                            }
                        }

                        if (!(await ensureHeaderSaved())) {
                            state.barcodeInput = '';
                            return;
                        }

                        await services.createSecondaryData(unitPrice, quantity, null, product.id, state.id, StorageManager.getUserId());
                        await methods.populateSecondaryData(state.id);
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