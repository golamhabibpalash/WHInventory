const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: null,
            id: '',
            name: '',
            slug: '',
            isActive: true,
            adminEmail: '',
            adminPassword: '',
            adminFirstName: '',
            adminLastName: '',
            errors: { name: '', slug: '', adminEmail: '', adminPassword: '' },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.slug = '';
            state.isActive = true;
            state.adminEmail = '';
            state.adminPassword = '';
            state.adminFirstName = '';
            state.adminLastName = '';
            state.errors = { name: '', slug: '', adminEmail: '', adminPassword: '' };
        };

        const validateForm = () => {
            state.errors = { name: '', slug: '', adminEmail: '', adminPassword: '' };
            let isValid = true;

            if (state.deleteMode) return true;

            if (!state.name) {
                state.errors.name = 'Organisation name is required.';
                isValid = false;
            }
            if (!state.slug) {
                state.errors.slug = 'Address is required.';
                isValid = false;
            } else if (!/^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/.test(state.slug)) {
                state.errors.slug = 'Use lowercase letters, digits and hyphens only.';
                isValid = false;
            }

            // The administrator is only collected when the tenant is first created.
            if (!state.id) {
                if (!state.adminEmail) {
                    state.errors.adminEmail = 'Administrator email is required.';
                    isValid = false;
                } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(state.adminEmail)) {
                    state.errors.adminEmail = 'Enter a valid email address.';
                    isValid = false;
                }
                if (!state.adminPassword) {
                    state.errors.adminPassword = 'Administrator password is required.';
                    isValid = false;
                } else if (state.adminPassword.length < 6) {
                    state.errors.adminPassword = 'Use at least 6 characters.';
                    isValid = false;
                }
            }

            return isValid;
        };

        const services = {
            getMainData: async () => await AxiosManager.get('/Tenant/GetTenantList', {}),
            createMainData: async (payload) => await AxiosManager.post('/Tenant/CreateTenant', payload),
            updateMainData: async (payload) => await AxiosManager.post('/Tenant/UpdateTenant', payload),
            deleteMainData: async (id, deletedById) => await AxiosManager.post('/Tenant/DeleteTenant', { id, deletedById }),
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map(item => ({
                    ...item,
                    createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null
                }));
            },
        };

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;

                    if (!validateForm()) return;

                    const userId = StorageManager.getUserId();
                    const response = state.deleteMode
                        ? await services.deleteMainData(state.id, userId)
                        : state.id === ''
                            ? await services.createMainData({
                                name: state.name,
                                slug: state.slug,
                                adminEmail: state.adminEmail,
                                adminPassword: state.adminPassword,
                                adminFirstName: state.adminFirstName,
                                adminLastName: state.adminLastName,
                                createdById: userId
                            })
                            : await services.updateMainData({
                                id: state.id,
                                name: state.name,
                                slug: state.slug,
                                isActive: state.isActive,
                                updatedById: userId
                            });

                    if (response.data.code === 200) {
                        const created = !state.deleteMode && state.id === '';
                        await methods.populateMainData();
                        mainGrid.refresh();
                        mainModal.obj.hide();

                        Swal.fire({
                            icon: 'success',
                            title: state.deleteMode ? 'Tenant Deleted' : created ? 'Tenant Created' : 'Tenant Updated',
                            text: created
                                ? `${state.name} is ready. Its administrator can sign in with ${state.adminEmail}.`
                                : undefined,
                            timer: created ? undefined : 2000,
                            showConfirmButton: created
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Save Failed',
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
                await SecurityManager.authorizePage(['Tenants']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                mainGrid.create(state.mainData);
                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', resetFormState);
            } catch (e) {
                // Surfaced rather than swallowed: a failure here leaves an empty page with no clue.
                console.error('TenantList failed to initialise', e);
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
        });

        const mainGrid = {
            obj: null,
            create: (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '360px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'name', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'name', headerText: 'Organisation', width: 220, minWidth: 200 },
                        { field: 'slug', headerText: 'Address', width: 160, minWidth: 140 },
                        { field: 'userCount', headerText: 'Users', width: 100, textAlign: 'Right' },
                        {
                            field: 'isActive', headerText: 'Status', width: 120, textAlign: 'Center',
                            // String template, not a function: EJ2 runs column templates through its
                            // own engine, and a function leaves the grid rendering nothing at all.
                            template: '${if(isActive)}<span class="badge bg-success">Active</span>${else}<span class="badge bg-secondary">Inactive</span>${/if}'
                        },
                        { field: 'createdAtUtc', headerText: 'Created', width: 160, format: 'dd/MM/yyyy HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['name', 'slug', 'userCount', 'isActive', 'createdAtUtc']);
                    },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'],
                            mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'],
                            mainGrid.obj.getSelectedRecords().length === 1);
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            resetFormState();
                            state.deleteMode = false;
                            state.mainTitle = 'Add Tenant';
                            mainModal.obj.show();
                        }

                        const selected = mainGrid.obj.getSelectedRecords()[0];

                        if (args.item.id === 'EditCustom' && selected) {
                            state.deleteMode = false;
                            state.mainTitle = 'Edit Tenant';
                            state.id = selected.id ?? '';
                            state.name = selected.name ?? '';
                            state.slug = selected.slug ?? '';
                            state.isActive = selected.isActive ?? true;
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom' && selected) {
                            state.deleteMode = true;
                            state.mainTitle = 'Delete Tenant?';
                            state.id = selected.id ?? '';
                            state.name = selected.name ?? '';
                            state.slug = selected.slug ?? '';
                            state.isActive = selected.isActive ?? true;
                            mainModal.obj.show();
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

        return { mainGridRef, mainModalRef, state, handler };
    }
};

Vue.createApp(App).mount('#app');
