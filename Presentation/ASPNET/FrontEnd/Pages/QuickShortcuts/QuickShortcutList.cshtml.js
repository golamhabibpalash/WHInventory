const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            icon: '',
            url: '',
            sortOrder: 0,
            errors: {
                name: '',
                icon: '',
                url: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);

        Vue.watch(() => state.name, () => { state.errors.name = ''; });
        Vue.watch(() => state.icon, () => { state.errors.icon = ''; });
        Vue.watch(() => state.url, () => { state.errors.url = ''; });

        const validateForm = function () {
            state.errors.name = '';
            state.errors.icon = '';
            state.errors.url = '';

            let isValid = true;

            if (!state.name) {
                state.errors.name = I18n.t('common.nameRequired');
                isValid = false;
            }
            if (!state.icon) {
                state.errors.icon = I18n.t('quickShortcut.iconRequired');
                isValid = false;
            }
            if (!state.url) {
                state.errors.url = I18n.t('quickShortcut.linkRequired');
                isValid = false;
            } else if (!state.url.startsWith('/')) {
                state.errors.url = "Link must start with '/', e.g. /PurchaseOrders/PurchaseOrderList.";
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.icon = '';
            state.url = '';
            state.sortOrder = 0;
            state.errors = {
                name: '',
                icon: '',
                url: ''
            };
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/QuickShortcut/GetQuickShortcutList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (name, icon, url, sortOrder, createdById) => {
                try {
                    const response = await AxiosManager.post('/QuickShortcut/CreateQuickShortcut', {
                        name, icon, url, sortOrder, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, icon, url, sortOrder, updatedById) => {
                try {
                    const response = await AxiosManager.post('/QuickShortcut/UpdateQuickShortcut', {
                        id, name, icon, url, sortOrder, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/QuickShortcut/DeleteQuickShortcut', {
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
                        ? await services.createMainData(state.name, state.icon, state.url, state.sortOrder || 0, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.name, state.icon, state.url, state.sortOrder || 0, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = I18n.t('quickShortcut.editTitle');
                            state.id = response?.data?.content?.data.id ?? '';
                            state.name = response?.data?.content?.data.name ?? '';
                            state.icon = response?.data?.content?.data.icon ?? '';
                            state.url = response?.data?.content?.data.url ?? '';
                            state.sortOrder = response?.data?.content?.data.sortOrder ?? 0;

                            Swal.fire({
                                icon: 'success',
                                title: I18n.t('common.saveSuccessful'),
                                text: I18n.t('common.formWillClose'),
                                timer: 1000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 1000);

                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: I18n.t('common.deleteSuccessful'),
                                text: I18n.t('common.formWillClose'),
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
                await SecurityManager.authorizePage(['QuickShortcuts']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });

            } catch (e) {
            } finally {

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
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        {
                            field: 'icon', headerText: I18n.t('quickShortcut.icon'), width: 90, minWidth: 90, textAlign: 'Center'
                        },
                        { field: 'name', headerText: I18n.t('common.name'), width: 200, minWidth: 200 },
                        { field: 'url', headerText: I18n.t('quickShortcut.link'), width: 260, minWidth: 260 },
                        { field: 'sortOrder', headerText: I18n.t('common.sortOrder'), width: 110, minWidth: 110, textAlign: 'Center' },
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
                    // The Icon column shows the actual glyph rather than its raw class name.
                    // queryCellInfo patches the cell's DOM directly instead of using a column
                    // `template` — a Syncfusion vanilla-JS Grid `template` string has repeatedly
                    // broken rendering elsewhere in this app (see Products/ProductList.cshtml.js),
                    // so queryCellInfo is the standard, proven-safe alternative here too.
                    queryCellInfo: (args) => {
                        if (args.column.field === 'icon') {
                            const iconClass = (args.data.icon || '').replace(/"/g, '');
                            args.cell.innerHTML = `<i class="${iconClass}" style="font-size:1.05rem;color:var(--primary);" title="${iconClass}"></i>`;
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['name', 'url', 'sortOrder', 'createdAtUtc']);
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
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = I18n.t('quickShortcut.addTitle');
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = I18n.t('quickShortcut.editTitle');
                                state.id = selectedRecord.id ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.icon = selectedRecord.icon ?? '';
                                state.url = selectedRecord.url ?? '';
                                state.sortOrder = selectedRecord.sortOrder ?? 0;
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = I18n.t('quickShortcut.deleteTitle');
                                state.id = selectedRecord.id ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.icon = selectedRecord.icon ?? '';
                                state.url = selectedRecord.url ?? '';
                                state.sortOrder = selectedRecord.sortOrder ?? 0;
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
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        return {
            mainGridRef,
            mainModalRef,
            nameRef,
            state,
            handler,
            t: I18n.t,
        };
    }
};

Vue.createApp(App).mount('#app');
