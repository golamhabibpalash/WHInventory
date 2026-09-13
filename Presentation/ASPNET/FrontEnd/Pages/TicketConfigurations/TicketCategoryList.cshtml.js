const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            description: '',
            sortOrder: 0,
            isActive: true,
            errors: { name: '' },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);

        Vue.watch(() => state.name, () => { state.errors.name = ''; });

        const validateForm = function () {
            state.errors.name = '';
            let isValid = true;
            if (!state.name) {
                state.errors.name = I18n.t('common.nameRequired');
                isValid = false;
            }
            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.description = '';
            state.sortOrder = 0;
            state.isActive = true;
            state.errors = { name: '' };
        };

        const services = {
            getMainData: async () => AxiosManager.get('/TicketCategory/GetTicketCategoryList', {}),
            createMainData: async (name, description, sortOrder, isActive, createdById) =>
                AxiosManager.post('/TicketCategory/CreateTicketCategory', { name, description, sortOrder, isActive, createdById }),
            updateMainData: async (id, name, description, sortOrder, isActive, updatedById) =>
                AxiosManager.post('/TicketCategory/UpdateTicketCategory', { id, name, description, sortOrder, isActive, updatedById }),
            deleteMainData: async (id, deletedById) =>
                AxiosManager.post('/TicketCategory/DeleteTicketCategory', { id, deletedById }),
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
        };

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;

                    if (!state.deleteMode && !validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.name, state.description, state.sortOrder || 0, state.isActive, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.name, state.description, state.sortOrder || 0, state.isActive, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({
                            icon: 'success',
                            title: state.deleteMode ? I18n.t('common.deleteSuccessful') : I18n.t('common.saveSuccessful'),
                            text: I18n.t('common.formWillClose'),
                            timer: 1000,
                            showConfirmButton: false
                        });
                        setTimeout(() => {
                            mainModal.obj.hide();
                            resetFormState();
                        }, 1000);
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode ? I18n.t('common.deleteFailed') : I18n.t('common.saveFailed'),
                            text: response.data.message ?? I18n.t('common.checkYourData'),
                            confirmButtonText: I18n.t('common.tryAgain')
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: I18n.t('common.errorOccurred'),
                        text: error.response?.data?.message ?? I18n.t('common.pleaseTryAgain'),
                        confirmButtonText: I18n.t('common.ok')
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TicketConfigurations']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });
            } catch (e) {
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
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
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'sortOrder', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'name', headerText: I18n.t('common.name'), width: 200, minWidth: 200 },
                        { field: 'description', headerText: I18n.t('common.description'), width: 300, minWidth: 300 },
                        { field: 'sortOrder', headerText: I18n.t('common.sortOrder'), width: 110, textAlign: 'Center' },
                        { field: 'isActive', headerText: I18n.t('common.active'), width: 100, textAlign: 'Center', displayAsCheckBox: true },
                        { field: 'createdAtUtc', headerText: I18n.t('common.createdAtUtc'), width: 150, format: 'dd/MM/yyyy HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: I18n.t('common.add'), tooltipText: I18n.t('common.add'), prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: I18n.t('common.edit'), tooltipText: I18n.t('common.edit'), prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: I18n.t('common.delete'), tooltipText: I18n.t('common.delete'), prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['name', 'description', 'sortOrder', 'isActive', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) mainGrid.obj.clearSelection();
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') mainGrid.obj.excelExport();

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = I18n.t('ticketCategory.addTitle');
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = I18n.t('ticketCategory.editTitle');
                                state.id = r.id ?? '';
                                state.name = r.name ?? '';
                                state.description = r.description ?? '';
                                state.sortOrder = r.sortOrder ?? 0;
                                state.isActive = r.isActive ?? true;
                                mainModal.obj.show();
                            }
                        }
                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = I18n.t('ticketCategory.deleteTitle');
                                state.id = r.id ?? '';
                                state.name = r.name ?? '';
                                mainModal.obj.show();
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

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, { backdrop: 'static', keyboard: false });
            }
        };

        return { mainGridRef, mainModalRef, nameRef, state, handler, t: I18n.t };
    }
};

Vue.createApp(App).mount('#app');
