const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            manageUsersTitle: '',
            roleName: '',
            roleUsers: [],
        });

        const mainGridRef = Vue.ref(null);
        const manageUsersModalRef = Vue.ref(null);

        const changedUsers = Vue.reactive(new Set());

        const userModules = Vue.computed(() => {
            const groups = {};
            let totalUsers = 0;

            for (const user of state.roleUsers) {
                const letter = (user.firstName || user.email || '?')[0].toUpperCase();
                if (!groups[letter]) {
                    groups[letter] = { letter, users: [], grantedCount: 0, allGranted: false };
                }
                groups[letter].users.push(user);
                totalUsers++;
            }

            const list = Object.values(groups)
                .map(g => {
                    g.grantedCount = g.users.filter(u => u.accessGranted).length;
                    g.allGranted = g.grantedCount === g.users.length && g.users.length > 0;
                    return g;
                })
                .sort((a, b) => a.letter.localeCompare(b.letter));

            return { list, totalUsers };
        });

        const userGrantedCount = Vue.computed(() => {
            return state.roleUsers.filter(u => u.accessGranted).length;
        });

        const hasUnsavedUserChanges = Vue.computed(() => {
            return changedUsers.size > 0;
        });

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Security/GetRoleList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getRoleUsers: async (roleName) => {
                try {
                    const response = await AxiosManager.post('/Security/GetRoleUsers', { roleName });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateUserRole: async (userId, roleName, accessGranted) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateUserRole', { userId, roleName, accessGranted });
                    return response;
                } catch (error) {
                    throw error;
                }
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
                    sortSettings: { columns: [{ field: 'name', direction: 'Ascending' }] },
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
                        { field: 'name', headerText: 'Role Name', width: 300, minWidth: 300 },
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Manage Users', tooltipText: 'Manage users for this role', prefixIcon: 'e-user', id: 'ManageUsersCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['ManageUsersCustom'], false);
                        mainGrid.obj.autoFitColumns(['name']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['ManageUsersCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['ManageUsersCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['ManageUsersCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['ManageUsersCustom'], false);
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

                        if (args.item.id === 'ManageUsersCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.manageUsersTitle = 'Manage Users — ' + (selectedRecord.name ?? '');
                                state.roleName = selectedRecord.name ?? '';
                                await methods.populateRoleUsers(state.roleName);
                                manageUsersModal.obj.show();
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

        const methods = {
            populateMainData: async () => {
                try {
                    const response = await services.getMainData();
                    state.mainData = response?.data?.content?.data;
                } catch (error) {
                    state.mainData = [];
                }
            },
            populateRoleUsers: async (roleName) => {
                try {
                    const response = await services.getRoleUsers(roleName);
                    state.roleUsers = response?.data?.content?.data ?? [];
                    changedUsers.clear();
                } catch (error) {
                    state.roleUsers = [];
                }
            },
        };

        const handler = {
            onUserToggle: (user) => {
                changedUsers.add(user.userId);
            },
            toggleUserGroup: (group, checked) => {
                for (const user of group.users) {
                    user.accessGranted = checked;
                    changedUsers.add(user.userId);
                }
            },
            grantAllUsers: async () => {
                const confirm = await Swal.fire({
                    icon: 'warning',
                    title: 'Are you sure?',
                    text: 'This will grant this role to ALL users.',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, proceed',
                    cancelButtonText: 'Cancel'
                });

                if (!confirm.isConfirmed) return;

                try {
                    for (const user of state.roleUsers) {
                        if (!user.accessGranted) {
                            await services.updateUserRole(user.userId, state.roleName, true);
                        }
                    }
                    await methods.populateRoleUsers(state.roleName);
                    Swal.fire({ icon: 'success', title: 'Role Granted to All Users' });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                }
            },
            revokeAllUsers: async () => {
                const confirm = await Swal.fire({
                    icon: 'warning',
                    title: 'Are you sure?',
                    text: 'This will remove this role from ALL users.',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, proceed',
                    cancelButtonText: 'Cancel'
                });

                if (!confirm.isConfirmed) return;

                try {
                    for (const user of state.roleUsers) {
                        if (user.accessGranted) {
                            await services.updateUserRole(user.userId, state.roleName, false);
                        }
                    }
                    await methods.populateRoleUsers(state.roleName);
                    Swal.fire({ icon: 'success', title: 'Role Revoked from All Users' });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                }
            },
            saveUserChanges: async () => {
                if (changedUsers.size === 0) return;

                try {
                    const usersToUpdate = Array.from(changedUsers);
                    let allSuccess = true;

                    for (const userId of usersToUpdate) {
                        const user = state.roleUsers.find(u => u.userId === userId);
                        if (!user) continue;
                        const response = await services.updateUserRole(userId, state.roleName, user.accessGranted);
                        if (response.data.code !== 200) {
                            allSuccess = false;
                        }
                    }

                    await methods.populateRoleUsers(state.roleName);

                    if (allSuccess) {
                        Swal.fire({ icon: 'success', title: 'Users Updated', timer: 1000, showConfirmButton: false });
                    } else {
                        Swal.fire({ icon: 'warning', title: 'Partial Update', text: 'Some users could not be updated.', confirmButtonText: 'OK' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                }
            },
        };

        const manageUsersModal = {
            obj: null,
            create: () => {
                manageUsersModal.obj = new bootstrap.Modal(manageUsersModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Roles']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                manageUsersModal.create();
            } catch (e) {
            } finally {
                
            }
        });

        return {
            state,
            mainGridRef,
            manageUsersModalRef,
            handler,
            userModules,
            userGrantedCount,
            hasUnsavedUserChanges,
        };
    }
};

Vue.createApp(App).mount('#app');
