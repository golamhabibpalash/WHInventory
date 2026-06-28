const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            code: '',
            description: '',
            priority: 0,
            isActive: true,
            effectiveFrom: null,
            effectiveTo: null,
            errors: {
                name: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);
        const codeRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);
        const effectiveFromRef = Vue.ref(null);
        const effectiveToRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PricePolicy/GetPricePolicyList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (name, code, description, priority, isActive, effectiveFrom, effectiveTo, createdById) => {
                try {
                    const response = await AxiosManager.post('/PricePolicy/CreatePricePolicy', {
                        name, code, description, priority, isActive, effectiveFrom, effectiveTo, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, code, description, priority, isActive, effectiveFrom, effectiveTo, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PricePolicy/UpdatePricePolicy', {
                        id, name, code, description, priority, isActive, effectiveFrom, effectiveTo, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PricePolicy/DeletePricePolicy', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
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
            }
        };

        const nameText = {
            obj: null,
            create: () => {
                nameText.obj = new ej.inputs.TextBox({ placeholder: 'Enter Name' });
                nameText.obj.appendTo(nameRef.value);
            },
            refresh: () => { if (nameText.obj) nameText.obj.value = state.name; }
        };

        const codeText = {
            obj: null,
            create: () => {
                codeText.obj = new ej.inputs.TextBox({ placeholder: 'Enter Code' });
                codeText.obj.appendTo(codeRef.value);
            },
            refresh: () => { if (codeText.obj) codeText.obj.value = state.code; }
        };

        const priorityNumber = {
            obj: null,
            create: () => {
                priorityNumber.obj = new ej.inputs.NumericTextBox({
                    placeholder: 'Priority',
                    format: 'n0',
                    min: 0,
                    value: 0,
                    change: (e) => { state.priority = e.value ?? 0; }
                });
                priorityNumber.obj.appendTo(priorityRef.value);
            },
            refresh: () => { if (priorityNumber.obj) priorityNumber.obj.value = state.priority; }
        };

        const effectiveFromPicker = {
            obj: null,
            create: () => {
                effectiveFromPicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'Select date',
                    format: 'yyyy-MM-dd',
                    change: (e) => { state.effectiveFrom = e.value; }
                });
                effectiveFromPicker.obj.appendTo(effectiveFromRef.value);
            },
            refresh: () => { if (effectiveFromPicker.obj) effectiveFromPicker.obj.value = state.effectiveFrom ? new Date(state.effectiveFrom) : null; }
        };

        const effectiveToPicker = {
            obj: null,
            create: () => {
                effectiveToPicker.obj = new ej.calendars.DatePicker({
                    placeholder: 'Select date',
                    format: 'yyyy-MM-dd',
                    change: (e) => { state.effectiveTo = e.value; }
                });
                effectiveToPicker.obj.appendTo(effectiveToRef.value);
            },
            refresh: () => { if (effectiveToPicker.obj) effectiveToPicker.obj.value = state.effectiveTo ? new Date(state.effectiveTo) : null; }
        };

        Vue.watch(() => state.name, () => { state.errors.name = ''; nameText.refresh(); });
        Vue.watch(() => state.code, () => { codeText.refresh(); });
        Vue.watch(() => state.priority, () => { priorityNumber.refresh(); });
        Vue.watch(() => state.effectiveFrom, () => { effectiveFromPicker.refresh(); });
        Vue.watch(() => state.effectiveTo, () => { effectiveToPicker.refresh(); });

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    let isValid = true;
                    if (!state.name) { state.errors.name = 'Name is required.'; isValid = false; }
                    if (!isValid) return;

                    const response = state.id === ''
                        ? await services.createMainData(state.name, state.code, state.description, state.priority, state.isActive, state.effectiveFrom, state.effectiveTo, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.name, state.code, state.description, state.priority, state.isActive, state.effectiveFrom, state.effectiveTo, StorageManager.getUserId());

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
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.code = '';
            state.description = '';
            state.priority = 0;
            state.isActive = true;
            state.effectiveFrom = null;
            state.effectiveTo = null;
            state.errors = { name: '' };
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
                        { field: 'name', headerText: 'Name', width: 200, minWidth: 200 },
                        { field: 'code', headerText: 'Code', width: 120 },
                        { field: 'priority', headerText: 'Priority', width: 100 },
                        { field: 'isActive', headerText: 'Active', width: 100, displayAsCheckBox: true },
                        { field: 'effectiveFrom', headerText: 'From', width: 120, format: 'yyyy-MM-dd' },
                        { field: 'effectiveTo', headerText: 'To', width: 120, format: 'yyyy-MM-dd' },
                        { field: 'description', headerText: 'Description', width: 300 },
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
                        mainGrid.obj.autoFitColumns(['name', 'code', 'priority', 'isActive', 'effectiveFrom', 'effectiveTo', 'createdAtUtc']);
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
                            state.mainTitle = 'Add Price Policy';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Price Policy';
                                state.id = selectedRecord.id ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.code = selectedRecord.code ?? '';
                                state.description = selectedRecord.description ?? '';
                                state.priority = selectedRecord.priority ?? 0;
                                state.isActive = selectedRecord.isActive ?? true;
                                state.effectiveFrom = selectedRecord.effectiveFrom ?? null;
                                state.effectiveTo = selectedRecord.effectiveTo ?? null;
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Price Policy?';
                                state.id = selectedRecord.id ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.code = selectedRecord.code ?? '';
                                state.description = selectedRecord.description ?? '';
                                state.priority = selectedRecord.priority ?? 0;
                                state.isActive = selectedRecord.isActive ?? true;
                                state.effectiveFrom = selectedRecord.effectiveFrom ?? null;
                                state.effectiveTo = selectedRecord.effectiveTo ?? null;
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
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PricePolicies']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                nameText.create();
                codeText.create();
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
            nameRef,
            codeRef,
            priorityRef,
            effectiveFromRef,
            effectiveToRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');
