const App = {
    setup() {
        const CALC_METHODS = [
            { id: 0, name: 'Fixed Price' },
            { id: 1, name: 'Cost + Markup %' },
            { id: 2, name: 'Cost + Markup Amount' },
            { id: 3, name: 'Gross Margin %' },
            { id: 4, name: 'Formula Based (× WAC)' },
        ];

        const state = Vue.reactive({
            mainData: [],
            qbData: [],
            productListLookupData: [],
            pricePolicyListLookupData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            productId: null,
            pricePolicyId: null,
            calculationMethod: 0,
            fixedPrice: null,
            markupPercent: null,
            markupAmount: null,
            marginPercent: null,
            formulaMultiplier: null,
            minimumSellingPrice: null,
            maximumDiscountPercent: null,
            effectiveFrom: null,
            effectiveTo: null,
            priority: 0,
            isActive: true,
            errors: { productId: '' },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const qbGridRef = Vue.ref(null);
        const productIdRef = Vue.ref(null);
        const pricePolicyIdRef = Vue.ref(null);
        const calculationMethodRef = Vue.ref(null);
        const fixedPriceRef = Vue.ref(null);
        const markupPercentRef = Vue.ref(null);
        const markupAmountRef = Vue.ref(null);
        const marginPercentRef = Vue.ref(null);
        const formulaMultiplierRef = Vue.ref(null);
        const minimumSellingPriceRef = Vue.ref(null);
        const maximumDiscountPercentRef = Vue.ref(null);
        const effectiveFromRef = Vue.ref(null);
        const effectiveToRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);

        const services = {
            getMainData: async () => AxiosManager.get('/ProductPrice/GetProductPriceList', {}),
            createMainData: async (data) => AxiosManager.post('/ProductPrice/CreateProductPrice', data),
            updateMainData: async (data) => AxiosManager.post('/ProductPrice/UpdateProductPrice', data),
            deleteMainData: async (id, deletedById) => AxiosManager.post('/ProductPrice/DeleteProductPrice', { id, deletedById }),
            getProductListLookupData: async () => AxiosManager.get('/Product/GetProductList', {}),
            getPricePolicyListLookupData: async () => AxiosManager.get('/PricePolicy/GetPricePolicyList', {}),
            getQbData: async (productPriceId) => AxiosManager.get('/ProductPrice/GetQuantityBreakList?productPriceId=' + productPriceId, {}),
            createQbData: async (data) => AxiosManager.post('/ProductPrice/CreateQuantityBreak', data),
            updateQbData: async (data) => AxiosManager.post('/ProductPrice/UpdateQuantityBreak', data),
            deleteQbData: async (id, deletedById) => AxiosManager.post('/ProductPrice/DeleteQuantityBreak', { id, deletedById }),
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc),
                    effectiveFrom: item.effectiveFrom ? new Date(item.effectiveFrom) : null,
                    effectiveTo: item.effectiveTo ? new Date(item.effectiveTo) : null,
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
            populateQbData: async (productPriceId) => {
                const response = await services.getQbData(productPriceId);
                state.qbData = response?.data?.content?.data ?? [];
            },
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

        const calculationMethodLookup = {
            obj: null,
            create: () => {
                calculationMethodLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: CALC_METHODS,
                    fields: { value: 'id', text: 'name' },
                    value: 0,
                    change: (e) => { state.calculationMethod = e.value ?? 0; }
                });
                calculationMethodLookup.obj.appendTo(calculationMethodRef.value);
            },
            refresh: () => { if (calculationMethodLookup.obj) calculationMethodLookup.obj.value = state.calculationMethod; }
        };

        const mkNumeric = (ref, stateKey) => ({
            obj: null,
            create: function () {
                this.obj = new ej.inputs.NumericTextBox({
                    placeholder: '0.00',
                    format: 'n4',
                    min: 0,
                    change: (e) => { state[stateKey] = e.value; }
                });
                this.obj.appendTo(ref.value);
            },
            refresh: function () { if (this.obj) this.obj.value = state[stateKey]; }
        });

        const fixedPriceNumber = mkNumeric(fixedPriceRef, 'fixedPrice');
        const markupPercentNumber = mkNumeric(markupPercentRef, 'markupPercent');
        const markupAmountNumber = mkNumeric(markupAmountRef, 'markupAmount');
        const marginPercentNumber = mkNumeric(marginPercentRef, 'marginPercent');
        const formulaMultiplierNumber = mkNumeric(formulaMultiplierRef, 'formulaMultiplier');
        const minimumSellingPriceNumber = mkNumeric(minimumSellingPriceRef, 'minimumSellingPrice');
        const maximumDiscountPercentNumber = mkNumeric(maximumDiscountPercentRef, 'maximumDiscountPercent');

        const priorityNumber = {
            obj: null,
            create: () => {
                priorityNumber.obj = new ej.inputs.NumericTextBox({
                    placeholder: '0', format: 'n0', min: 0, value: 0,
                    change: (e) => { state.priority = e.value ?? 0; }
                });
                priorityNumber.obj.appendTo(priorityRef.value);
            },
            refresh: () => { if (priorityNumber.obj) priorityNumber.obj.value = state.priority; }
        };

        const mkDatePicker = (ref, stateKey) => ({
            obj: null,
            create: function () {
                this.obj = new ej.calendars.DatePicker({
                    placeholder: 'Select date', format: 'yyyy-MM-dd',
                    change: (e) => { state[stateKey] = e.value; }
                });
                this.obj.appendTo(ref.value);
            },
            refresh: function () {
                if (this.obj) this.obj.value = state[stateKey] ? new Date(state[stateKey]) : null;
            }
        });

        const effectiveFromPicker = mkDatePicker(effectiveFromRef, 'effectiveFrom');
        const effectiveToPicker = mkDatePicker(effectiveToRef, 'effectiveTo');

        Vue.watch(() => state.productId, () => { state.errors.productId = ''; productIdLookup.refresh(); });
        Vue.watch(() => state.pricePolicyId, () => { pricePolicyIdLookup.refresh(); });
        Vue.watch(() => state.calculationMethod, () => { calculationMethodLookup.refresh(); });
        Vue.watch(() => state.priority, () => { priorityNumber.refresh(); });
        Vue.watch(() => state.effectiveFrom, () => { effectiveFromPicker.refresh(); });
        Vue.watch(() => state.effectiveTo, () => { effectiveToPicker.refresh(); });

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    let isValid = true;
                    if (!state.productId) { state.errors.productId = 'Product is required.'; isValid = false; }
                    if (!isValid) return;

                    const payload = {
                        productId: state.productId,
                        pricePolicyId: state.pricePolicyId,
                        calculationMethod: state.calculationMethod,
                        fixedPrice: state.fixedPrice,
                        markupPercent: state.markupPercent,
                        markupAmount: state.markupAmount,
                        marginPercent: state.marginPercent,
                        formulaMultiplier: state.formulaMultiplier,
                        minimumSellingPrice: state.minimumSellingPrice,
                        maximumDiscountPercent: state.maximumDiscountPercent,
                        effectiveFrom: state.effectiveFrom,
                        effectiveTo: state.effectiveTo,
                        priority: state.priority,
                        isActive: state.isActive,
                    };

                    const response = state.id === ''
                        ? await services.createMainData({ ...payload, createdById: StorageManager.getUserId() })
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData({ ...payload, id: state.id, updatedById: StorageManager.getUserId() });

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: state.deleteMode ? 'Delete Successful' : 'Save Successful',
                            text: 'Form will be closed...',
                            timer: 2000,
                            showConfirmButton: false
                        });
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
            addQuantityBreak: async () => {
                if (!state.id) return;
                qbGrid.obj.addRecord();
            },
        };

        const resetFormState = () => {
            state.id = '';
            state.productId = null;
            state.pricePolicyId = null;
            state.calculationMethod = 0;
            state.fixedPrice = null;
            state.markupPercent = null;
            state.markupAmount = null;
            state.marginPercent = null;
            state.formulaMultiplier = null;
            state.minimumSellingPrice = null;
            state.maximumDiscountPercent = null;
            state.effectiveFrom = null;
            state.effectiveTo = null;
            state.priority = 0;
            state.isActive = true;
            state.qbData = [];
            state.errors = { productId: '' };
        };

        const qbGrid = {
            obj: null,
            create: () => {
                qbGrid.obj = new ej.grids.Grid({
                    height: '150px',
                    dataSource: state.qbData,
                    allowFiltering: false,
                    allowSorting: true,
                    allowResizing: true,
                    editSettings: { allowAdding: true, allowEditing: true, allowDeleting: true, mode: 'Normal' },
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'minQuantity', headerText: 'Min Qty', width: 100, defaultValue: '1', editType: 'numericedit', edit: { params: { decimals: 0, min: 1 } } },
                        { field: 'maxQuantity', headerText: 'Max Qty', width: 100, editType: 'numericedit', edit: { params: { decimals: 0 } } },
                        { field: 'price', headerText: 'Price', width: 120, defaultValue: '0', editType: 'numericedit', edit: { params: { decimals: 4, min: 0 } } },
                    ],
                    toolbar: [],
                    actionBegin: async (args) => {
                        if (args.requestType === 'save') {
                            const row = args.data;
                            const payload = {
                                productPriceId: state.id,
                                minQuantity: row.minQuantity ?? 1,
                                maxQuantity: row.maxQuantity ?? null,
                                price: row.price ?? 0,
                            };
                            if (!row.id) {
                                await services.createQbData({ ...payload, createdById: StorageManager.getUserId() });
                            } else {
                                await services.updateQbData({ ...payload, id: row.id, updatedById: StorageManager.getUserId() });
                            }
                            await methods.populateQbData(state.id);
                        }
                        if (args.requestType === 'delete') {
                            const row = args.data[0];
                            if (row?.id) {
                                await services.deleteQbData(row.id, StorageManager.getUserId());
                                await methods.populateQbData(state.id);
                            }
                        }
                    },
                });
                qbGrid.obj.appendTo(qbGridRef.value);
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
                        { field: 'productName', headerText: 'Product', width: 200, minWidth: 200 },
                        { field: 'pricePolicyName', headerText: 'Price Policy', width: 150 },
                        { field: 'calculationMethodName', headerText: 'Method', width: 160 },
                        { field: 'fixedPrice', headerText: 'Fixed Price', width: 120, format: 'N4' },
                        { field: 'markupPercent', headerText: 'Markup %', width: 110 },
                        { field: 'marginPercent', headerText: 'Margin %', width: 110 },
                        { field: 'minimumSellingPrice', headerText: 'Min Price', width: 110, format: 'N4' },
                        { field: 'maximumDiscountPercent', headerText: 'Max Disc %', width: 120 },
                        { field: 'priority', headerText: 'Priority', width: 90 },
                        { field: 'isActive', headerText: 'Active', width: 90, displayAsCheckBox: true },
                        { field: 'effectiveFrom', headerText: 'From', width: 110, format: 'yyyy-MM-dd' },
                        { field: 'effectiveTo', headerText: 'To', width: 110, format: 'yyyy-MM-dd' },
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
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowSelecting: () => { if (mainGrid.obj.getSelectedRecords().length) mainGrid.obj.clearSelection(); },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') { mainGrid.obj.excelExport(); }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Product Price';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Product Price';
                                state.id = r.id ?? '';
                                state.productId = r.productId ?? null;
                                state.pricePolicyId = r.pricePolicyId ?? null;
                                state.calculationMethod = r.calculationMethod ?? 0;
                                state.fixedPrice = r.fixedPrice ?? null;
                                state.markupPercent = r.markupPercent ?? null;
                                state.markupAmount = r.markupAmount ?? null;
                                state.marginPercent = r.marginPercent ?? null;
                                state.formulaMultiplier = r.formulaMultiplier ?? null;
                                state.minimumSellingPrice = r.minimumSellingPrice ?? null;
                                state.maximumDiscountPercent = r.maximumDiscountPercent ?? null;
                                state.effectiveFrom = r.effectiveFrom ?? null;
                                state.effectiveTo = r.effectiveTo ?? null;
                                state.priority = r.priority ?? 0;
                                state.isActive = r.isActive ?? true;
                                await methods.populateQbData(r.id);
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Product Price?';
                                state.id = r.id ?? '';
                                state.productId = r.productId ?? null;
                                state.calculationMethod = r.calculationMethod ?? 0;
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
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, { backdrop: 'static', keyboard: false });
                mainModalRef.value.addEventListener('shown.bs.modal', () => {
                    if (!qbGrid.obj) {
                        qbGrid.create();
                    } else {
                        qbGrid.obj.dataSource = state.qbData;
                        qbGrid.obj.refresh();
                    }
                });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['ProductPrices']);
                await SecurityManager.validateToken();
                await methods.populateProductListLookupData();
                await methods.populatePricePolicyListLookupData();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                productIdLookup.create();
                pricePolicyIdLookup.create();
                calculationMethodLookup.create();
                fixedPriceNumber.create();
                markupPercentNumber.create();
                markupAmountNumber.create();
                marginPercentNumber.create();
                formulaMultiplierNumber.create();
                minimumSellingPriceNumber.create();
                maximumDiscountPercentNumber.create();
                priorityNumber.create();
                effectiveFromPicker.create();
                effectiveToPicker.create();
                mainModal.create();
            } catch (e) {
            } finally {
            }
        });

        return {
            mainGridRef,
            mainModalRef,
            qbGridRef,
            productIdRef,
            pricePolicyIdRef,
            calculationMethodRef,
            fixedPriceRef,
            markupPercentRef,
            markupAmountRef,
            marginPercentRef,
            formulaMultiplierRef,
            minimumSellingPriceRef,
            maximumDiscountPercentRef,
            effectiveFromRef,
            effectiveToRef,
            priorityRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');
