const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            productListLookupData: [],
            pricePolicyListLookupData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            code: '',
            description: '',
            productId: null,
            pricePolicyId: null,
            promotionalPrice: null,
            discountPercent: null,
            startDate: null,
            endDate: null,
            priority: 0,
            isActive: true,
            errors: { name: '', productId: '' },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);
        const codeRef = Vue.ref(null);
        const productIdRef = Vue.ref(null);
        const pricePolicyIdRef = Vue.ref(null);
        const promotionalPriceRef = Vue.ref(null);
        const discountPercentRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);
        const startDateRef = Vue.ref(null);
        const endDateRef = Vue.ref(null);

        const services = {
            getMainData: async () => AxiosManager.get('/Promotion/GetPromotionList', {}),
            createMainData: async (data) => AxiosManager.post('/Promotion/CreatePromotion', data),
            updateMainData: async (data) => AxiosManager.post('/Promotion/UpdatePromotion', data),
            deleteMainData: async (id, deletedById) => AxiosManager.post('/Promotion/DeletePromotion', { id, deletedById }),
            getProductListLookupData: async () => AxiosManager.get('/Product/GetProductList', {}),
            getPricePolicyListLookupData: async () => AxiosManager.get('/PricePolicy/GetPricePolicyList', {}),
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc),
                    startDate: item.startDate ? new Date(item.startDate) : null,
                    endDate: item.endDate ? new Date(item.endDate) : null,
                }));
            },
            populateProductListLookupData: async () => {
                const response = await services.getProductListLookupData();
                state.productListLookupData = response?.data?.content?.data ?? [];
                if (productIdLookup.obj) productIdLookup.obj.setProperties({ dataSource: state.productListLookupData });
            },
            populatePricePolicyListLookupData: async () => {
                const response = await services.getPricePolicyListLookupData();
                state.pricePolicyListLookupData = response?.data?.content?.data ?? [];
                if (pricePolicyIdLookup.obj) pricePolicyIdLookup.obj.setProperties({ dataSource: state.pricePolicyListLookupData });
            },
        };

        const nameText = {
            obj: null,
            create: () => { nameText.obj = new ej.inputs.TextBox({ placeholder: 'Enter Name' }); nameText.obj.appendTo(nameRef.value); },
            refresh: () => { if (nameText.obj) nameText.obj.value = state.name; }
        };

        const codeText = {
            obj: null,
            create: () => { codeText.obj = new ej.inputs.TextBox({ placeholder: 'Enter Code' }); codeText.obj.appendTo(codeRef.value); },
            refresh: () => { if (codeText.obj) codeText.obj.value = state.code; }
        };

        const productIdLookup = {
            obj: null,
            create: () => {
                productIdLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select Product',
                    change: (e) => { state.productId = e.value; }
                });
                productIdLookup.obj.appendTo(productIdRef.value);
            },
            refresh: () => { if (productIdLookup.obj) productIdLookup.obj.value = state.productId; }
        };

        const pricePolicyIdLookup = {
            obj: null,
            create: () => {
                pricePolicyIdLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.pricePolicyListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select Policy (optional)',
                    showClearButton: true,
                    change: (e) => { state.pricePolicyId = e.value ?? null; }
                });
                pricePolicyIdLookup.obj.appendTo(pricePolicyIdRef.value);
            },
            refresh: () => { if (pricePolicyIdLookup.obj) pricePolicyIdLookup.obj.value = state.pricePolicyId; }
        };

        const promotionalPriceNumber = {
            obj: null,
            create: () => {
                promotionalPriceNumber.obj = new ej.inputs.NumericTextBox({ placeholder: '0.00', format: 'n4', min: 0, change: (e) => { state.promotionalPrice = e.value; } });
                promotionalPriceNumber.obj.appendTo(promotionalPriceRef.value);
            },
            refresh: () => { if (promotionalPriceNumber.obj) promotionalPriceNumber.obj.value = state.promotionalPrice; }
        };

        const discountPercentNumber = {
            obj: null,
            create: () => {
                discountPercentNumber.obj = new ej.inputs.NumericTextBox({ placeholder: '0.00', format: 'n2', min: 0, max: 100, change: (e) => { state.discountPercent = e.value; } });
                discountPercentNumber.obj.appendTo(discountPercentRef.value);
            },
            refresh: () => { if (discountPercentNumber.obj) discountPercentNumber.obj.value = state.discountPercent; }
        };

        const priorityNumber = {
            obj: null,
            create: () => {
                priorityNumber.obj = new ej.inputs.NumericTextBox({ placeholder: '0', format: 'n0', min: 0, value: 0, change: (e) => { state.priority = e.value ?? 0; } });
                priorityNumber.obj.appendTo(priorityRef.value);
            },
            refresh: () => { if (priorityNumber.obj) priorityNumber.obj.value = state.priority; }
        };

        const startDatePicker = {
            obj: null,
            create: () => {
                startDatePicker.obj = new ej.calendars.DatePicker({ placeholder: 'Select date', format: 'yyyy-MM-dd', change: (e) => { state.startDate = e.value; } });
                startDatePicker.obj.appendTo(startDateRef.value);
            },
            refresh: () => { if (startDatePicker.obj) startDatePicker.obj.value = state.startDate ? new Date(state.startDate) : null; }
        };

        const endDatePicker = {
            obj: null,
            create: () => {
                endDatePicker.obj = new ej.calendars.DatePicker({ placeholder: 'Select date', format: 'yyyy-MM-dd', change: (e) => { state.endDate = e.value; } });
                endDatePicker.obj.appendTo(endDateRef.value);
            },
            refresh: () => { if (endDatePicker.obj) endDatePicker.obj.value = state.endDate ? new Date(state.endDate) : null; }
        };

        Vue.watch(() => state.name, () => { state.errors.name = ''; nameText.refresh(); });
        Vue.watch(() => state.code, () => { codeText.refresh(); });
        Vue.watch(() => state.productId, () => { state.errors.productId = ''; productIdLookup.refresh(); });
        Vue.watch(() => state.pricePolicyId, () => { pricePolicyIdLookup.refresh(); });
        Vue.watch(() => state.promotionalPrice, () => { promotionalPriceNumber.refresh(); });
        Vue.watch(() => state.discountPercent, () => { discountPercentNumber.refresh(); });
        Vue.watch(() => state.priority, () => { priorityNumber.refresh(); });
        Vue.watch(() => state.startDate, () => { startDatePicker.refresh(); });
        Vue.watch(() => state.endDate, () => { endDatePicker.refresh(); });

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    let isValid = true;
                    if (!state.name) { state.errors.name = 'Name is required.'; isValid = false; }
                    if (!state.productId) { state.errors.productId = 'Product is required.'; isValid = false; }
                    if (!isValid) return;

                    const payload = {
                        name: state.name, code: state.code, description: state.description,
                        productId: state.productId, pricePolicyId: state.pricePolicyId,
                        promotionalPrice: state.promotionalPrice, discountPercent: state.discountPercent,
                        startDate: state.startDate, endDate: state.endDate,
                        priority: state.priority, isActive: state.isActive,
                    };

                    const response = state.id === ''
                        ? await services.createMainData({ ...payload, createdById: StorageManager.getUserId() })
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData({ ...payload, id: state.id, updatedById: StorageManager.getUserId() });

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({ icon: 'success', title: state.deleteMode ? 'Delete Successful' : 'Save Successful', text: 'Form will be closed...', timer: 2000, showConfirmButton: false });
                        setTimeout(() => { mainModal.obj.hide(); resetFormState(); }, 2000);
                    } else {
                        Swal.fire({ icon: 'error', title: state.deleteMode ? 'Delete Failed' : 'Save Failed', text: response.data.message ?? 'Please check your data.', confirmButtonText: 'Try Again' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const resetFormState = () => {
            state.id = ''; state.name = ''; state.code = ''; state.description = '';
            state.productId = null; state.pricePolicyId = null;
            state.promotionalPrice = null; state.discountPercent = null;
            state.startDate = null; state.endDate = null;
            state.priority = 0; state.isActive = true;
            state.errors = { name: '', productId: '' };
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
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'name', headerText: 'Name', width: 200 },
                        { field: 'code', headerText: 'Code', width: 100 },
                        { field: 'productName', headerText: 'Product', width: 180 },
                        { field: 'pricePolicyName', headerText: 'Price Policy', width: 140 },
                        { field: 'promotionalPrice', headerText: 'Promo Price', width: 120, format: 'N4' },
                        { field: 'discountPercent', headerText: 'Disc %', width: 90 },
                        { field: 'startDate', headerText: 'Start', width: 110, format: 'yyyy-MM-dd' },
                        { field: 'endDate', headerText: 'End', width: 110, format: 'yyyy-MM-dd' },
                        { field: 'priority', headerText: 'Priority', width: 90 },
                        { field: 'isActive', headerText: 'Active', width: 80, displayAsCheckBox: true },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () { mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false); },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], mainGrid.obj.getSelectedRecords().length == 1);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], mainGrid.obj.getSelectedRecords().length == 1);
                    },
                    rowSelecting: () => { if (mainGrid.obj.getSelectedRecords().length) mainGrid.obj.clearSelection(); },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') { mainGrid.obj.excelExport(); }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Promotion';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Promotion';
                                state.id = r.id ?? '';
                                state.name = r.name ?? '';
                                state.code = r.code ?? '';
                                state.description = r.description ?? '';
                                state.productId = r.productId ?? null;
                                state.pricePolicyId = r.pricePolicyId ?? null;
                                state.promotionalPrice = r.promotionalPrice ?? null;
                                state.discountPercent = r.discountPercent ?? null;
                                state.startDate = r.startDate ?? null;
                                state.endDate = r.endDate ?? null;
                                state.priority = r.priority ?? 0;
                                state.isActive = r.isActive ?? true;
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Promotion?';
                                state.id = r.id ?? '';
                                state.name = r.name ?? '';
                                state.productId = r.productId ?? null;
                                state.priority = r.priority ?? 0;
                                state.isActive = r.isActive ?? true;
                                mainModal.obj.show();
                            }
                        }
                    }
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => { mainGrid.obj.setProperties({ dataSource: state.mainData }); }
        };

        const mainModal = {
            obj: null,
            create: () => { mainModal.obj = new bootstrap.Modal(mainModalRef.value, { backdrop: 'static', keyboard: false }); }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Promotions']);
                await SecurityManager.validateToken();
                await methods.populateProductListLookupData();
                await methods.populatePricePolicyListLookupData();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                nameText.create();
                codeText.create();
                productIdLookup.create();
                pricePolicyIdLookup.create();
                promotionalPriceNumber.create();
                discountPercentNumber.create();
                priorityNumber.create();
                startDatePicker.create();
                endDatePicker.create();
                mainModal.create();
            } catch (e) {
            } finally {
            }
        });

        return {
            mainGridRef, mainModalRef, nameRef, codeRef, productIdRef, pricePolicyIdRef,
            promotionalPriceRef, discountPercentRef, priorityRef, startDateRef, endDateRef,
            state, handler,
        };
    }
};

Vue.createApp(App).mount('#app');
