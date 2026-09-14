const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            manageUsersTitle: '',
            roleName: '',
            roleUsers: [],
        });

        const manageUsersModalRef = Vue.ref(null);

        const changedUsers = Vue.reactive(new Set());

        // ── Module grouping configuration (same as UserList) ───────────
        const moduleConfig = [
            { name: 'Dashboards', icon: 'fas fa-tachometer-alt', roles: ['Dashboards'] },
            { name: 'Sales', icon: 'fas fa-chart-line', roles: ['CustomerGroups', 'CustomerCategories', 'Customers', 'CustomerContacts', 'SalesOrders', 'SalesReports', 'Warranties'] },
            { name: 'Purchase', icon: 'fas fa-shopping-cart', roles: ['VendorGroups', 'VendorCategories', 'Vendors', 'VendorContacts', 'PurchaseOrders', 'PurchaseReports'] },
            { name: 'Inventory', icon: 'fas fa-warehouse', roles: ['UnitMeasures', 'ProductGroups', 'Brands', 'Products', 'Warehouses', 'DeliveryOrders', 'SalesReturns', 'GoodsReceives', 'PurchaseReturns', 'TransferOuts', 'TransferIns', 'PositiveAdjustments', 'NegativeAdjustments', 'Scrappings', 'StockCounts', 'TransactionReports', 'StockReports', 'MovementReports'] },
            { name: 'Pricing', icon: 'fas fa-tags', roles: ['PricePolicies', 'ProductPrices', 'Promotions', 'PriceReports'] },
            { name: 'Utilities', icon: 'fas fa-tools', roles: ['Todos', 'TodoItems'] },
            { name: 'Membership', icon: 'fas fa-users', roles: ['Users', 'Roles'] },
            { name: 'Profiles', icon: 'fas fa-user-circle', roles: ['Profiles'] },
            { name: 'Ticketing', icon: 'fas fa-ticket-alt', roles: ['Tickets', 'TicketAgent'] },
            { name: 'Settings', icon: 'fas fa-cog', roles: ['Companies', 'Taxs', 'NumberSequences', 'QuickShortcuts', 'TicketConfigurations'] },
            { name: 'Logs', icon: 'fas fa-history', roles: ['Tenants', 'AuditLogs', 'UserActivityLogs'] },
        ];

        function formatRoleName(roleName) {
            return roleName
                .replace(/([A-Z])/g, ' $1')
                .replace(/^./, s => s.toUpperCase())
                .trim();
        }

        const roleModules = Vue.computed(() => {
            const modules = [];
            let totalRoles = 0;

            for (const cfg of moduleConfig) {
                const roleItems = cfg.roles
                    .map(roleName => {
                        const found = state.mainData.find(r => r.name === roleName);
                        return found
                            ? { roleName: found.name, displayName: formatRoleName(found.name) }
                            : null;
                    })
                    .filter(Boolean);

                if (roleItems.length === 0) continue;

                totalRoles += roleItems.length;
                modules.push({
                    name: cfg.name,
                    icon: cfg.icon,
                    roles: roleItems,
                });
            }

            return { list: modules, totalRoles };
        });

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

        const methods = {
            populateMainData: async () => {
                try {
                    const response = await services.getMainData();
                    state.mainData = response?.data?.content?.data ?? [];
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
            openManageUsers: async (roleName) => {
                state.manageUsersTitle = 'Manage Users — ' + formatRoleName(roleName);
                state.roleName = roleName;
                await methods.populateRoleUsers(roleName);
                manageUsersModal.obj.show();
            },
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
                    await SecurityManager.refreshSession();
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
                    await SecurityManager.refreshSession();
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
                        await SecurityManager.refreshSession();
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
                await SecurityManager.authorizePage(['Roles', 'Users']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                manageUsersModal.create();
            } catch (e) {
            } finally {
                
            }
        });

        return {
            state,
            manageUsersModalRef,
            handler,
            roleModules,
            userModules,
            userGrantedCount,
            hasUnsavedUserChanges,
        };
    }
};

Vue.createApp(App).mount('#app');
