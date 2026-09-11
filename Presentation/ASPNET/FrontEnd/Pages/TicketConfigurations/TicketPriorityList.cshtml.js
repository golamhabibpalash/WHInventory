const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            colorHex: '#1b84ff',
            level: 0,
            slaResponseHours: null,
            slaResolutionHours: null,
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
                state.errors.name = 'Name is required.';
                isValid = false;
            }
            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.colorHex = '#1b84ff';
            state.level = 0;
            state.slaResponseHours = null;
            state.slaResolutionHours = null;
            state.isActive = true;
            state.errors = { name: '' };
        };

        const services = {
            getMainData: async () => AxiosManager.get('/TicketPriority/GetTicketPriorityList', {}),
            createMainData: async (payload) => AxiosManager.post('/TicketPriority/CreateTicketPriority', payload),
            updateMainData: async (payload) => AxiosManager.post('/TicketPriority/UpdateTicketPriority', payload),
            deleteMainData: async (id, deletedById) => AxiosManager.post('/TicketPriority/DeleteTicketPriority', { id, deletedById }),
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

                    const basePayload = {
                        name: state.name,
                        colorHex: state.colorHex,
                        level: state.level || 0,
                        slaResponseHours: state.slaResponseHours || null,
                        slaResolutionHours: state.slaResolutionHours || null,
                        isActive: state.isActive
                    };

                    const response = state.id === ''
                        ? await services.createMainData({ ...basePayload, createdById: StorageManager.getUserId() })
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData({ id: state.id, ...basePayload, updatedById: StorageManager.getUserId() });

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({
                            icon: 'success',
                            title: state.deleteMode ? 'Delete Successful' : 'Save Successful',
                            text: 'Form will be closed...',
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
                    sortSettings: { columns: [{ field: 'level', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'name', headerText: 'Name', width: 150 },
                        { field: 'level', headerText: 'Level', width: 90, textAlign: 'Center' },
                        { field: 'slaResponseHours', headerText: 'SLA Response (h)', width: 150, textAlign: 'Center' },
                        { field: 'slaResolutionHours', headerText: 'SLA Resolution (h)', width: 160, textAlign: 'Center' },
                        { field: 'isActive', headerText: 'Active', width: 100, textAlign: 'Center', displayAsCheckBox: true },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'dd/MM/yyyy HH:mm' }
                    ],
                    // Colored swatch for a priority's ColorHex — queryCellInfo (not a column
                    // `template`) per this app's established Syncfusion Grid safe pattern.
                    queryCellInfo: (args) => {
                        if (args.column.field === 'name') {
                            const color = args.data.colorHex || '#6c757d';
                            args.cell.innerHTML = `<span style="display:inline-block;width:.65rem;height:.65rem;border-radius:50%;background:${color};margin-right:.4rem;"></span>${args.data.name ?? ''}`;
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['name', 'level', 'slaResponseHours', 'slaResolutionHours', 'isActive', 'createdAtUtc']);
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
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                    ],
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') mainGrid.obj.excelExport();

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Ticket Priority';
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Ticket Priority';
                                state.id = r.id ?? '';
                                state.name = r.name ?? '';
                                state.colorHex = r.colorHex || '#1b84ff';
                                state.level = r.level ?? 0;
                                state.slaResponseHours = r.slaResponseHours ?? null;
                                state.slaResolutionHours = r.slaResolutionHours ?? null;
                                state.isActive = r.isActive ?? true;
                                mainModal.obj.show();
                            }
                        }
                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Ticket Priority?';
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

        return { mainGridRef, mainModalRef, nameRef, state, handler };
    }
};

Vue.createApp(App).mount('#app');
