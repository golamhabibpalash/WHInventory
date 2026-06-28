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
            showComplexDiv: true,
            isNewlyCreated: false,
            isSubmitting: false,
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
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const vendorIdRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const vendorQuickModalRef = Vue.ref(null);
        const vendorQuickGroupIdRef = Vue.ref(null);
        const vendorQuickCategoryIdRef = Vue.ref(null);
        const vendorGroupQuickModalRef = Vue.ref(null);
        const vendorCategoryQuickModalRef = Vue.ref(null);

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
            state.showComplexDiv = true;
            state.isNewlyCreated = false;
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
            createMainData: async (orderDate, description, orderStatus, taxId, vendorId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/CreatePurchaseOrder', {
                        orderDate, description, orderStatus, taxId, vendorId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, orderDate, description, orderStatus, taxId, vendorId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/UpdatePurchaseOrder', {
                        id, orderDate, description, orderStatus, taxId, vendorId, updatedById
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
                await new Promise(resolve => setTimeout(resolve, 200));

                if (!validateForm()) {
                    state.isSubmitting = false;
                    return;
                }

                const wasCreating = state.id === '';

                try {
                    const response = wasCreating
                        ? await services.createMainData(state.orderDate, state.description, state.orderStatus, state.taxId, state.vendorId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.orderDate, state.description, state.orderStatus, state.taxId, state.vendorId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Purchase Order';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.orderDate = response?.data?.content?.data.orderDate ? new Date(response.data.content.data.orderDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.vendorId = response?.data?.content?.data.vendorId ?? '';
                            state.taxId = response?.data?.content?.data.taxId ?? '';
                            taxListLookup.trackingChange = true;
                            state.orderStatus = String(response?.data?.content?.data.orderStatus ?? '');

                            if (wasCreating) {
                                state.showComplexDiv = true;
                                state.isNewlyCreated = true;
                                await methods.populateSecondaryData(state.id);
                                secondaryGrid.refresh();
                                Swal.fire({
                                    icon: 'success',
                                    title: 'Save Successful',
                                    text: 'You can now add items to this purchase order.',
                                    timer: 2000,
                                    showConfirmButton: false
                                });
                            } else {
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
                            }
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
            handleSubmitClose: () => {
                mainModal.obj.hide();
                resetFormState();
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
                            timer: 2000,
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
                        { field: 'orderDate', headerText: 'PO Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'vendorName', headerText: 'Vendor', width: 200, minWidth: 200 },
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
                            state.secondaryData = [];
                            secondaryGrid.refresh();
                            state.showComplexDiv = false;
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Purchase Order';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.vendorId = selectedRecord.vendorId ?? '';
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
                                state.mainTitle = 'Delete Purchase Order?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.vendorId = selectedRecord.vendorId ?? '';
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
                                window.open('/PurchaseOrders/PurchaseOrderPdf?id=' + (selectedRecord.id ?? ''), '_blank');
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

        let productObj, priceObj, quantityObj, totalObj, numberObj, remarkObj;

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
                                const product = (state.productListLookupData ?? []).find(item => item.id === data[field]);
                                return product ? `${product.name}` : '';
                            },
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
                                    productObj = new ej.dropdowns.DropDownList({
                                        dataSource: state.productListLookupData,
                                        fields: { value: 'id', text: 'name' },
                                        value: args.rowData.productId,
                                        change: (e) => {
                                            const selectedProduct = state.productListLookupData.find(item => item.id === e.value);
                                            if (selectedProduct) {
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
                                    quantityObj = new ej.inputs.NumericTextBox({
                                        value: args.rowData.quantity ?? 0,
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
                    beforeDataBound: () => { },
                    dataBound: function () { },
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
                            secondaryGrid.obj.excelExport({ fileName: `PurchaseOrderItems_${date}.xlsx` });
                        }
                    },
                    actionComplete: async (args) => {
                        if (args.requestType === 'save' && args.action === 'add') {
                            const purchaseOrderId = state.id;
                            const userId = StorageManager.getUserId();
                            const data = args.data;

                            await services.createSecondaryData(data?.unitPrice, data?.quantity, data?.remark, data?.productId, purchaseOrderId, userId);
                            await methods.populateSecondaryData(purchaseOrderId);
                            secondaryGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'save' && args.action === 'edit') {
                            const purchaseOrderId = state.id;
                            const userId = StorageManager.getUserId();
                            const data = args.data;

                            await services.updateSecondaryData(data?.id, data?.unitPrice, data?.quantity, data?.remark, data?.productId, purchaseOrderId, userId);
                            await methods.populateSecondaryData(purchaseOrderId);
                            secondaryGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'delete') {
                            const purchaseOrderId = state.id;
                            const userId = StorageManager.getUserId();
                            const data = args.data[0];

                            await services.deleteSecondaryData(data?.id, userId);
                            await methods.populateSecondaryData(purchaseOrderId);
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
                if (secondaryGrid.obj) secondaryGrid.obj.setProperties({ dataSource: state.secondaryData });
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
                mainModalRef.value.addEventListener('shown.bs.modal', async () => {
                    if (!secondaryGrid.obj) {
                        await secondaryGrid.create(state.secondaryData);
                    } else {
                        secondaryGrid.refresh();
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
                    vendorGroupListLookupQuick.create();
                    vendorCategoryListLookupQuick.create();
                    vendorListLookup.create();
                    taxListLookup.create();
                    purchaseOrderStatusListLookup.create();
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
            secondaryGridRef,
            vendorQuickModalRef,
            vendorQuickGroupIdRef,
            vendorQuickCategoryIdRef,
            vendorGroupQuickModalRef,
            vendorCategoryQuickModalRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit,
                handleSubmitClose: methods.handleSubmitClose,
                openVendorQuickCreate: vendorQuickHandler.open,
                closeVendorQuickCreate: vendorQuickHandler.close,
                submitVendorQuickCreate: vendorQuickHandler.submit,
                openVendorGroupQuickCreate: vendorGroupQuickHandler.open,
                closeVendorGroupQuickCreate: vendorGroupQuickHandler.close,
                submitVendorGroupQuickCreate: vendorGroupQuickHandler.submit,
                openVendorCategoryQuickCreate: vendorCategoryQuickHandler.open,
                closeVendorCategoryQuickCreate: vendorCategoryQuickHandler.close,
                submitVendorCategoryQuickCreate: vendorCategoryQuickHandler.submit,
            }
        };
    }
};

Vue.createApp(App).mount('#app');