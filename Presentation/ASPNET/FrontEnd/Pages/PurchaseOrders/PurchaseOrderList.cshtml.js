const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            vendorListLookupData: [],
            taxListLookupData: [],
            purchaseOrderStatusListLookupData: [],
            secondaryData: [],
            productListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            orderDate: new Date(),
            description: '',
            referenceNumber: '',
            vendorId: null,
            taxId: null,
            orderStatus: null,
            errors: {
                orderDate: '',
                vendorId: '',
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
                number: ''
            },
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00',
            amountInWords: '',
            vendorGroupListLookupData: [],
            vendorCategoryListLookupData: [],
            vendorQuickName: '',
            vendorQuickCountry: '',
            vendorQuickVendorGroupId: null,
            vendorQuickVendorCategoryId: null,
            vendorQuickStreet: '',
            vendorQuickCity: '',
            vendorQuickState: '',
            vendorQuickZipCode: '',
            vendorQuickPhoneNumber: '',
            vendorQuickEmailAddress: '',
            vendorQuickIsSubmitting: false,
            vendorQuickErrors: {
                name: '',
                vendorGroupId: '',
                vendorCategoryId: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                phoneNumber: '',
                emailAddress: '',
            },
            vendorGroupQuickName: '',
            vendorGroupQuickDescription: '',
            vendorGroupQuickIsSubmitting: false,
            vendorGroupQuickErrors: { name: '' },
            vendorCategoryQuickName: '',
            vendorCategoryQuickDescription: '',
            vendorCategoryQuickIsSubmitting: false,
            vendorCategoryQuickErrors: { name: '' },
            taxQuickName: '',
            taxQuickPercentage: null,
            taxQuickDescription: '',
            taxQuickIsSubmitting: false,
            taxQuickErrors: { name: '', percentage: '' },
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const vendorIdRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const productPickRef = Vue.ref(null);
        const vendorQuickModalRef = Vue.ref(null);
        const vendorQuickGroupIdRef = Vue.ref(null);
        const vendorQuickCategoryIdRef = Vue.ref(null);
        const vendorGroupQuickModalRef = Vue.ref(null);
        const vendorCategoryQuickModalRef = Vue.ref(null);
        const taxQuickModalRef = Vue.ref(null);

        // Running line total for the "Select Product" form.
        const posLineTotal = Vue.computed(() => (state.productPick.unitPrice || 0) * (state.productPick.quantity || 0));

        const formatQty = (value) => Number(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });

        const resetProductPick = () => {
            state.productPick = { productId: null, unitPrice: 0, quantity: 1 };
            state.productHint = { name: '', number: '' };
            if (productPickLookup.obj) {
                productPickLookup.obj.value = null;
                productPickLookup.obj.text = '';
            }
        };

        const validateForm = function () {
            state.errors.orderDate = '';
            state.errors.vendorId = '';
            state.errors.taxId = '';
            state.errors.orderStatus = '';

            let isValid = true;

            if (!state.orderDate) {
                state.errors.orderDate = 'Order date is required.';
                isValid = false;
            }
            if (!state.vendorId) {
                state.errors.vendorId = 'Vendor is required.';
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
            state.referenceNumber = '';
            state.vendorId = null;
            state.taxId = null;
            state.orderStatus = null;
            state.errors = {
                orderDate: '',
                vendorId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            };
            state.secondaryData = [];
            state.subTotalAmount = '0.00';
            state.taxAmount = '0.00';
            state.totalAmount = '0.00';
            state.amountInWords = '';
            resetProductPick();
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (orderDate, description, referenceNumber, orderStatus, taxId, vendorId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/CreatePurchaseOrder', {
                        orderDate, description, referenceNumber, orderStatus, taxId, vendorId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, orderDate, description, referenceNumber, orderStatus, taxId, vendorId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/UpdatePurchaseOrder', {
                        id, orderDate, description, referenceNumber, orderStatus, taxId, vendorId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/DeletePurchaseOrder', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Vendor/GetVendorList', {});
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
            getPurchaseOrderStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSecondaryData: async (purchaseOrderId) => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrderItem/GetPurchaseOrderItemByPurchaseOrderIdList?purchaseOrderId=' + purchaseOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createSecondaryData: async (unitPrice, quantity, remark, productId, purchaseOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/CreatePurchaseOrderItem', {
                        unitPrice, quantity, remark, productId, purchaseOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, unitPrice, quantity, remark, productId, purchaseOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/UpdatePurchaseOrderItem', {
                        id, unitPrice, quantity, remark, productId, purchaseOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/DeletePurchaseOrderItem', {
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
            createVendor: async (name, vendorGroupId, vendorCategoryId, street, city, state, zipCode, country, phoneNumber, emailAddress, createdById) => {
                try {
                    const response = await AxiosManager.post('/Vendor/CreateVendor', {
                        name, vendorGroupId, vendorCategoryId, street, city, state, zipCode, country,
                        phoneNumber, emailAddress, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorGroupListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/VendorGroup/GetVendorGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorCategoryListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/VendorCategory/GetVendorCategoryList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createVendorGroup: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/VendorGroup/CreateVendorGroup', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createVendorCategory: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/VendorCategory/CreateVendorCategory', { name, description, createdById });
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
            },
        };

        const methods = {
            populateVendorListLookupData: async () => {
                const response = await services.getVendorListLookupData();
                state.vendorListLookupData = response?.data?.content?.data;
            },
            populateTaxListLookupData: async () => {
                const response = await services.getTaxListLookupData();
                state.taxListLookupData = response?.data?.content?.data;
            },
            populatePurchaseOrderStatusListLookupData: async () => {
                const response = await services.getPurchaseOrderStatusListLookupData();
                state.purchaseOrderStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSecondaryData: async (purchaseOrderId) => {
                try {
                    const response = await services.getSecondaryData(purchaseOrderId);
                    state.secondaryData = response?.data?.content?.data.map(item => ({
                        ...item,
                        createdAtUtc: new Date(item.createdAtUtc)
                    }));
                    methods.refreshPaymentSummary(purchaseOrderId);
                } catch (error) {
                    state.secondaryData = [];
                }
            },
            populateProductListLookupData: async () => {
                const response = await services.getProductListLookupData();
                state.productListLookupData = response?.data?.content?.data ?? [];
            },
            populateVendorGroupListLookupData: async () => {
                const response = await services.getVendorGroupListLookupData();
                state.vendorGroupListLookupData = response?.data?.content?.data ?? [];
                if (vendorGroupListLookupQuick.obj) {
                    vendorGroupListLookupQuick.obj.setProperties({ dataSource: state.vendorGroupListLookupData });
                }
            },
            populateVendorCategoryListLookupData: async () => {
                const response = await services.getVendorCategoryListLookupData();
                state.vendorCategoryListLookupData = response?.data?.content?.data ?? [];
                if (vendorCategoryListLookupQuick.obj) {
                    vendorCategoryListLookupQuick.obj.setProperties({ dataSource: state.vendorCategoryListLookupData });
                }
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

                if (!validateForm()) {
                    state.isSubmitting = false;
                    return;
                }

                const wasCreating = state.id === '';

                try {
                    const response = wasCreating
                        ? await services.createMainData(state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendorId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendorId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.orderDate = response?.data?.content?.data.orderDate ? new Date(response.data.content.data.orderDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.referenceNumber = response?.data?.content?.data.referenceNumber ?? '';
                            state.vendorId = response?.data?.content?.data.vendorId ?? '';
                            state.taxId = response?.data?.content?.data.taxId ?? '';
                            taxListLookup.trackingChange = true;
                            state.orderStatus = String(response?.data?.content?.data.orderStatus ?? '');
                            state.mainTitle = `Purchase Order ${state.number}`;

                            if (wasCreating) {
                                // POS flow: stay on the same screen with the order now created,
                                // so the buyer can go straight to adding products.
                                await methods.populateSecondaryData(state.id);
                                Swal.fire({
                                    icon: 'success',
                                    title: 'Purchase Order Created',
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
                state.errors.vendorId = '';
                state.errors.taxId = '';
                state.errors.orderStatus = '';
                taxListLookup.trackingChange = false;
            }
        };

        const vendorListLookup = {
            obj: null,
            create: () => {
                if (state.vendorListLookupData && Array.isArray(state.vendorListLookupData)) {
                    vendorListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.vendorListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select a Vendor',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('name', 'startsWith', e.text, true);
                            }
                            e.updateData(state.vendorListLookupData, query);
                        },
                        change: (e) => {
                            state.vendorId = e.value;
                        }
                    });
                    vendorListLookup.obj.appendTo(vendorIdRef.value);
                }
            },
            refresh: () => {
                if (vendorListLookup.obj) {
                    vendorListLookup.obj.value = state.vendorId;
                }
            }
        };

        const vendorGroupListLookupQuick = {
            obj: null,
            create: () => {
                vendorGroupListLookupQuick.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.vendorGroupListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Vendor Group',
                    filterBarPlaceholder: 'Search',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        let query = new ej.data.Query();
                        if (e.text !== '') {
                            query = query.where('name', 'startsWith', e.text, true);
                        }
                        e.updateData(state.vendorGroupListLookupData, query);
                    },
                    change: (e) => {
                        state.vendorQuickVendorGroupId = e.value;
                        state.vendorQuickErrors.vendorGroupId = '';
                    }
                });
                vendorGroupListLookupQuick.obj.appendTo(vendorQuickGroupIdRef.value);
            }
        };

        const vendorCategoryListLookupQuick = {
            obj: null,
            create: () => {
                vendorCategoryListLookupQuick.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.vendorCategoryListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Vendor Category',
                    filterBarPlaceholder: 'Search',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        let query = new ej.data.Query();
                        if (e.text !== '') {
                            query = query.where('name', 'startsWith', e.text, true);
                        }
                        e.updateData(state.vendorCategoryListLookupData, query);
                    },
                    change: (e) => {
                        state.vendorQuickVendorCategoryId = e.value;
                        state.vendorQuickErrors.vendorCategoryId = '';
                    }
                });
                vendorCategoryListLookupQuick.obj.appendTo(vendorQuickCategoryIdRef.value);
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

        const purchaseOrderStatusListLookup = {
            obj: null,
            create: () => {
                if (state.purchaseOrderStatusListLookupData && Array.isArray(state.purchaseOrderStatusListLookupData)) {
                    purchaseOrderStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.purchaseOrderStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select an Order Status',
                        change: (e) => {
                            state.orderStatus = e.value;
                        }
                    });
                    purchaseOrderStatusListLookup.obj.appendTo(orderStatusRef.value);
                }
            },
            refresh: () => {
                if (purchaseOrderStatusListLookup.obj) {
                    purchaseOrderStatusListLookup.obj.value = state.orderStatus;
                }
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
            () => state.vendorId,
            (newVal, oldVal) => {
                vendorListLookup.refresh();
                state.errors.vendorId = '';
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
                purchaseOrderStatusListLookup.refresh();
                state.errors.orderStatus = '';
            }
        );

        const vendorQuickModal = {
            obj: null,
            create: () => {
                vendorQuickModal.obj = new bootstrap.Modal(vendorQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const vendorGroupQuickModal = {
            obj: null,
            create: () => {
                vendorGroupQuickModal.obj = new bootstrap.Modal(vendorGroupQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const vendorCategoryQuickModal = {
            obj: null,
            create: () => {
                vendorCategoryQuickModal.obj = new bootstrap.Modal(vendorCategoryQuickModalRef.value, {
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

        const vendorQuickHandler = {
            resetForm: () => {
                state.vendorQuickName = '';
                state.vendorQuickCountry = '';
                state.vendorQuickVendorGroupId = null;
                state.vendorQuickVendorCategoryId = null;
                state.vendorQuickStreet = '';
                state.vendorQuickCity = '';
                state.vendorQuickState = '';
                state.vendorQuickZipCode = '';
                state.vendorQuickPhoneNumber = '';
                state.vendorQuickEmailAddress = '';
                state.vendorQuickErrors = {
                    name: '', vendorGroupId: '', vendorCategoryId: '',
                    street: '', city: '', state: '', zipCode: '',
                    phoneNumber: '', emailAddress: ''
                };
                if (vendorGroupListLookupQuick.obj) vendorGroupListLookupQuick.obj.value = null;
                if (vendorCategoryListLookupQuick.obj) vendorCategoryListLookupQuick.obj.value = null;
            },
            open: async () => {
                vendorQuickHandler.resetForm();
                await methods.populateVendorGroupListLookupData();
                await methods.populateVendorCategoryListLookupData();
                vendorQuickModal.obj.show();
            },
            close: () => {
                vendorQuickModal.obj.hide();
            },
            submit: async () => {
                const errors = {
                    name: '', vendorGroupId: '', vendorCategoryId: '',
                    street: '', city: '', state: '', zipCode: '',
                    phoneNumber: '', emailAddress: ''
                };
                let isValid = true;

                if (!state.vendorQuickName) { errors.name = 'Name is required.'; isValid = false; }
                if (!state.vendorQuickVendorGroupId) { errors.vendorGroupId = 'Vendor Group is required.'; isValid = false; }
                if (!state.vendorQuickVendorCategoryId) { errors.vendorCategoryId = 'Vendor Category is required.'; isValid = false; }
                if (!state.vendorQuickStreet) { errors.street = 'Street is required.'; isValid = false; }
                if (!state.vendorQuickCity) { errors.city = 'City is required.'; isValid = false; }
                if (!state.vendorQuickState) { errors.state = 'State is required.'; isValid = false; }
                if (!state.vendorQuickZipCode) { errors.zipCode = 'Zip Code is required.'; isValid = false; }
                if (!state.vendorQuickPhoneNumber) { errors.phoneNumber = 'Phone Number is required.'; isValid = false; }
                if (!state.vendorQuickEmailAddress) { errors.emailAddress = 'Email Address is required.'; isValid = false; }

                state.vendorQuickErrors = errors;
                if (!isValid) return;

                try {
                    state.vendorQuickIsSubmitting = true;

                    const response = await services.createVendor(
                        state.vendorQuickName,
                        state.vendorQuickVendorGroupId,
                        state.vendorQuickVendorCategoryId,
                        state.vendorQuickStreet,
                        state.vendorQuickCity,
                        state.vendorQuickState,
                        state.vendorQuickZipCode,
                        state.vendorQuickCountry,
                        state.vendorQuickPhoneNumber,
                        state.vendorQuickEmailAddress,
                        StorageManager.getUserId()
                    );

                    if (response.data.code === 200) {
                        const newVendor = response.data.content.data;

                        // Refresh vendor dropdown data source and auto-select new vendor
                        await methods.populateVendorListLookupData();
                        vendorListLookup.obj.setProperties({
                            dataSource: state.vendorListLookupData,
                            value: newVendor.id
                        });
                        state.vendorId = newVendor.id;

                        vendorQuickModal.obj.hide();
                        Swal.fire({
                            icon: 'success',
                            title: 'Vendor Created',
                            text: `"${newVendor.name}" has been created and selected.`,
                            timer: 1000,
                            showConfirmButton: false
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Create Failed',
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
                    state.vendorQuickIsSubmitting = false;
                }
            }
        };

        const vendorGroupQuickHandler = {
            open: () => {
                state.vendorGroupQuickName = '';
                state.vendorGroupQuickDescription = '';
                state.vendorGroupQuickErrors = { name: '' };
                vendorQuickModal.obj.hide();
                setTimeout(() => vendorGroupQuickModal.obj.show(), 300);
            },
            close: () => {
                vendorGroupQuickModal.obj.hide();
                setTimeout(() => vendorQuickModal.obj.show(), 300);
            },
            submit: async () => {
                state.vendorGroupQuickErrors = { name: '' };
                if (!state.vendorGroupQuickName?.trim()) {
                    state.vendorGroupQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.vendorGroupQuickIsSubmitting = true;
                    const response = await services.createVendorGroup(
                        state.vendorGroupQuickName.trim(),
                        state.vendorGroupQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newGroup = response.data.content.data;
                        await methods.populateVendorGroupListLookupData();
                        vendorGroupListLookupQuick.obj.setProperties({ dataSource: state.vendorGroupListLookupData, value: newGroup.id });
                        state.vendorQuickVendorGroupId = newGroup.id;
                        vendorGroupQuickModal.obj.hide();
                        setTimeout(() => vendorQuickModal.obj.show(), 300);
                    } else {
                        state.vendorGroupQuickErrors.name = response.data.message ?? 'Failed to create vendor group.';
                    }
                } catch (error) {
                    state.vendorGroupQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.vendorGroupQuickIsSubmitting = false;
                }
            }
        };

        const vendorCategoryQuickHandler = {
            open: () => {
                state.vendorCategoryQuickName = '';
                state.vendorCategoryQuickDescription = '';
                state.vendorCategoryQuickErrors = { name: '' };
                vendorQuickModal.obj.hide();
                setTimeout(() => vendorCategoryQuickModal.obj.show(), 300);
            },
            close: () => {
                vendorCategoryQuickModal.obj.hide();
                setTimeout(() => vendorQuickModal.obj.show(), 300);
            },
            submit: async () => {
                state.vendorCategoryQuickErrors = { name: '' };
                if (!state.vendorCategoryQuickName?.trim()) {
                    state.vendorCategoryQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.vendorCategoryQuickIsSubmitting = true;
                    const response = await services.createVendorCategory(
                        state.vendorCategoryQuickName.trim(),
                        state.vendorCategoryQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newCategory = response.data.content.data;
                        await methods.populateVendorCategoryListLookupData();
                        vendorCategoryListLookupQuick.obj.setProperties({ dataSource: state.vendorCategoryListLookupData, value: newCategory.id });
                        state.vendorQuickVendorCategoryId = newCategory.id;
                        vendorCategoryQuickModal.obj.hide();
                        setTimeout(() => vendorQuickModal.obj.show(), 300);
                    } else {
                        state.vendorCategoryQuickErrors.name = response.data.message ?? 'Failed to create vendor category.';
                    }
                } catch (error) {
                    state.vendorCategoryQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.vendorCategoryQuickIsSubmitting = false;
                }
            }
        };

        const taxQuickHandler = {
            open: () => {
                state.taxQuickName = '';
                state.taxQuickPercentage = null;
                state.taxQuickDescription = '';
                state.taxQuickErrors = { name: '', percentage: '' };
                taxQuickModal.obj.show();
            },
            close: () => {
                taxQuickModal.obj.hide();
            },
            submit: async () => {
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

                        // Refresh tax dropdown data source and auto-select the new tax
                        await methods.populateTaxListLookupData();
                        taxListLookup.obj.setProperties({
                            dataSource: state.taxListLookupData,
                            value: newTax.id
                        });
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
                        Swal.fire({
                            icon: 'error',
                            title: 'Create Failed',
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
                    state.taxQuickIsSubmitting = false;
                }
            }
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
                        { field: 'orderDate', headerText: 'PO Date', width: 150, format: 'dd/MM/yyyy' },
                        { field: 'vendorName', headerText: 'Vendor', width: 200, minWidth: 200 },
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
                    created: () => {
                        const searchBar = document.getElementById('MainGrid_searchbar');
                        if (searchBar) {
                            searchBar.addEventListener('input', function () {
                                mainGrid.obj.search(this.value);
                            });
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
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
                            mainGrid.obj.excelExport({ fileName: `PurchaseOrders_${date}.xlsx` });
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Purchase Order';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            resetProductPick();
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = `Purchase Order ${selectedRecord.number ?? ''}`;
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.referenceNumber = selectedRecord.referenceNumber ?? '';
                                state.vendorId = selectedRecord.vendorId ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                taxListLookup.trackingChange = true;
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');

                                await methods.populateSecondaryData(selectedRecord.id);

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            resetProductPick();
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Purchase Order?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.vendorId = selectedRecord.vendorId ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');

                                await methods.populateSecondaryData(selectedRecord.id);

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/PurchaseOrders/PurchaseOrderPdf?id=' + (selectedRecord.id ?? ''), '_blank');
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

        // "Select Product" search box in the POS panel. Mirrors vendorListLookup: a Syncfusion
        // DropDownList over the product list.
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
                            state.productHint = { name: '', number: '' };
                            return;
                        }
                        state.productPick.productId = product.id;
                        state.productPick.unitPrice = product.unitPrice ?? 0;
                        state.productPick.quantity = 1;
                        state.productHint = { name: product.name, number: product.number ?? '' };
                    }
                });
                productPickLookup.obj.appendTo(productPickRef.value);
            }
        };

        // The POS screen adds line items against a saved header. If the buyer starts by picking
        // a product before saving, create the header on the fly from the order bar fields.
        const ensureHeaderSaved = async () => {
            if (state.id) return true;

            if (!validateForm()) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Complete the order details',
                    text: 'Vendor, tax, status and order date are required before adding products.'
                });
                return false;
            }

            try {
                const response = await services.createMainData(
                    state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendorId, StorageManager.getUserId());

                if (response.data.code === 200) {
                    const data = response.data.content.data;
                    state.id = data.id ?? '';
                    state.number = data.number ?? '';
                    state.mainTitle = `Purchase Order ${state.number}`;
                    taxListLookup.trackingChange = true;
                    await methods.populateMainData();
                    mainGrid.refresh();
                    return true;
                }

                Swal.fire({ icon: 'error', title: 'Save Failed', text: response.data.message ?? 'Could not create the purchase order.' });
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
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit cost' });
                    return;
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
                await persistLine(line, { quantity });
            },
            commitLinePrice: async (line, value) => {
                const unitPrice = Number(value);
                if (isNaN(unitPrice) || unitPrice < 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit cost' });
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
                await SecurityManager.authorizePage(['PurchaseOrders']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                orderDatePicker.create();
                numberText.create();

                Promise.all([
                    methods.populateVendorListLookupData(),
                    methods.populateTaxListLookupData(),
                    methods.populatePurchaseOrderStatusListLookupData(),
                    methods.populateProductListLookupData(),
                ]).then(() => {
                    vendorQuickModal.create();
                    vendorGroupQuickModal.create();
                    vendorCategoryQuickModal.create();
                    taxQuickModal.create();
                    vendorGroupListLookupQuick.create();
                    vendorCategoryListLookupQuick.create();
                    vendorListLookup.create();
                    taxListLookup.create();
                    purchaseOrderStatusListLookup.create();
                    productPickLookup.create();
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
            vendorIdRef,
            taxIdRef,
            orderStatusRef,
            productPickRef,
            posLineTotal,
            vendorQuickModalRef,
            vendorQuickGroupIdRef,
            vendorQuickCategoryIdRef,
            vendorGroupQuickModalRef,
            vendorCategoryQuickModalRef,
            taxQuickModalRef,
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
                openVendorQuickCreate: vendorQuickHandler.open,
                closeVendorQuickCreate: vendorQuickHandler.close,
                submitVendorQuickCreate: vendorQuickHandler.submit,
                openVendorGroupQuickCreate: vendorGroupQuickHandler.open,
                closeVendorGroupQuickCreate: vendorGroupQuickHandler.close,
                submitVendorGroupQuickCreate: vendorGroupQuickHandler.submit,
                openVendorCategoryQuickCreate: vendorCategoryQuickHandler.open,
                closeVendorCategoryQuickCreate: vendorCategoryQuickHandler.close,
                submitVendorCategoryQuickCreate: vendorCategoryQuickHandler.submit,
                openTaxQuickCreate: taxQuickHandler.open,
                closeTaxQuickCreate: taxQuickHandler.close,
                submitTaxQuickCreate: taxQuickHandler.submit,
            }
        };
    }
};

Vue.createApp(App).mount('#app');