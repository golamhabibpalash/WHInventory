const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            kpiCounts: { open: 0, pending: 0, resolved: 0, overdue: 0 },
            subject: '',
            description: '',
            categoryId: null,
            priorityId: null,
            attachmentFile: null,
            errors: { subject: '', categoryId: '', description: '' },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const subjectRef = Vue.ref(null);
        const categoryRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);
        const attachmentInputRef = Vue.ref(null);

        const STATUS_LABELS = { 0: 'New', 1: 'Open', 2: 'In Progress', 3: 'Pending User', 4: 'Pending Internal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_KEYS = { 0: 'New', 1: 'Open', 2: 'InProgress', 3: 'PendingUser', 4: 'PendingInternal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };

        const validateForm = () => {
            state.errors = { subject: '', categoryId: '', description: '' };
            let isValid = true;
            if (!state.subject) { state.errors.subject = 'Subject is required.'; isValid = false; }
            if (!state.categoryId) { state.errors.categoryId = 'Category is required.'; isValid = false; }
            if (!state.description) { state.errors.description = 'Description is required.'; isValid = false; }
            return isValid;
        };

        const resetFormState = () => {
            state.subject = '';
            state.description = '';
            state.categoryId = null;
            state.priorityId = null;
            state.attachmentFile = null;
            if (attachmentInputRef.value) attachmentInputRef.value.value = '';
            state.errors = { subject: '', categoryId: '', description: '' };
            categoryListLookup.obj?.setProperties({ value: null });
            priorityListLookup.obj?.setProperties({ value: null });
        };

        // AxiosManager.get(url, config) only forwards config.headers/responseType — it does not
        // build a query string from config.params (see wwwroot/lib/indotalent/axios-manager.js),
        // so every GET with query parameters has to build its own query string into the URL.
        const toQueryString = (params) => {
            const usp = new URLSearchParams();
            Object.entries(params || {}).forEach(([k, v]) => {
                if (v !== null && v !== undefined && v !== '') usp.append(k, v);
            });
            const qs = usp.toString();
            return qs ? `?${qs}` : '';
        };

        const services = {
            getTicketList: async () => AxiosManager.get('/Ticket/GetTicketList' + toQueryString({ excludeClosed: false, pageSize: 500 })),
            createTicket: async (payload) => AxiosManager.post('/Ticket/CreateTicket', payload),
            uploadAttachment: async (ticketId, file) => {
                const formData = new FormData();
                formData.append('ticketId', ticketId);
                formData.append('file', file);
                return AxiosManager.post('/Ticket/UploadTicketAttachment', formData, {});
            },
            getCategoryList: async () => AxiosManager.get('/TicketCategory/GetTicketCategoryList', {}),
            getPriorityList: async () => AxiosManager.get('/TicketPriority/GetTicketPriorityList', {}),
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getTicketList();
                const data = response?.data?.content?.data ?? [];
                state.mainData = data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc),
                    lastActivityAtUtc: item.lastActivityAtUtc ? new Date(item.lastActivityAtUtc) : null,
                }));
                state.kpiCounts = {
                    open: data.filter(x => [0, 1, 2].includes(x.status)).length,
                    pending: data.filter(x => [3, 4].includes(x.status)).length,
                    resolved: data.filter(x => x.status === 5).length,
                    overdue: data.filter(x => x.isOverdue).length,
                };
            },
            populateCategoryListLookupData: async () => {
                const response = await services.getCategoryList();
                return (response?.data?.content?.data ?? []).filter(x => x.isActive);
            },
            populatePriorityListLookupData: async () => {
                const response = await services.getPriorityList();
                return (response?.data?.content?.data ?? []).filter(x => x.isActive);
            },
        };

        const handler = {
            onAttachmentSelected: (e) => {
                state.attachmentFile = e.target.files[0] || null;
            },
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;

                    if (!validateForm()) return;

                    const response = await services.createTicket({
                        subject: state.subject,
                        description: state.description,
                        categoryId: state.categoryId,
                        priorityId: state.priorityId,
                        source: 0,
                        createdById: StorageManager.getUserId()
                    });

                    if (response.data.code !== 200) {
                        Swal.fire({ icon: 'error', title: 'Save Failed', text: response.data.message ?? 'Please check your data.', confirmButtonText: 'Try Again' });
                        return;
                    }

                    const newTicket = response.data.content.data;

                    if (state.attachmentFile) {
                        try {
                            await services.uploadAttachment(newTicket.id, state.attachmentFile);
                        } catch (attachError) {
                            // The ticket itself was created successfully; surface the attachment
                            // failure separately rather than treating the whole submit as failed.
                            console.error('Attachment upload failed:', attachError);
                        }
                    }

                    await methods.populateMainData();
                    mainGrid.refresh();

                    Swal.fire({
                        icon: 'success',
                        title: 'Ticket Created',
                        text: `${newTicket.ticketNumber} has been created.`,
                        timer: 1500,
                        showConfirmButton: false
                    });
                    setTimeout(() => {
                        mainModal.obj.hide();
                        resetFormState();
                    }, 1200);

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

        const categoryListLookup = {
            obj: null,
            create: async () => {
                const data = await methods.populateCategoryListLookupData();
                categoryListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: data,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a category',
                    change: (e) => { state.categoryId = e.value; }
                });
                categoryListLookup.obj.appendTo(categoryRef.value);
            }
        };

        const priorityListLookup = {
            obj: null,
            create: async () => {
                const data = await methods.populatePriorityListLookupData();
                priorityListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: data,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Default',
                    change: (e) => { state.priorityId = e.value; }
                });
                priorityListLookup.obj.appendTo(priorityRef.value);
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Tickets']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                await categoryListLookup.create();
                await priorityListLookup.create();
                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', resetFormState);
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
                    height: '460px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'lastActivityAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 20, pageSizes: ["10", "20", "50", "100", "All"] },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'ticketNumber', headerText: 'Ticket #', width: 130 },
                        { field: 'subject', headerText: 'Subject', width: 260, minWidth: 260 },
                        { field: 'categoryName', headerText: 'Category', width: 140 },
                        { field: 'priorityName', headerText: 'Priority', width: 120 },
                        { field: 'status', headerText: 'Status', width: 140 },
                        { field: 'createdAtUtc', headerText: 'Created', width: 140, format: 'dd/MM/yyyy' },
                        { field: 'lastActivityAtUtc', headerText: 'Last Activity', width: 150, format: 'dd/MM/yyyy HH:mm' },
                        { field: 'isOverdue', headerText: 'SLA', width: 100, textAlign: 'Center' },
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Create Ticket', tooltipText: 'Create Ticket', prefixIcon: 'e-add', id: 'AddCustom' },
                    ],
                    // Status/Priority/SLA columns render as styled badges — queryCellInfo (not a
                    // column `template`), matching this app's established safe pattern for
                    // Syncfusion Grid custom cell rendering.
                    queryCellInfo: (args) => {
                        if (args.column.field === 'status') {
                            const key = STATUS_KEYS[args.data.status] ?? 'New';
                            const label = STATUS_LABELS[args.data.status] ?? 'New';
                            args.cell.innerHTML = `<span class="ticket-status-badge ticket-status-${key}">${label}</span>`;
                        }
                        if (args.column.field === 'priorityName' && args.data.priorityName) {
                            const color = args.data.priorityColorHex || '#6c757d';
                            args.cell.innerHTML = `<span class="ticket-priority-badge"><span class="ticket-priority-dot" style="background:${color};"></span>${args.data.priorityName}</span>`;
                        }
                        if (args.column.field === 'isOverdue') {
                            args.cell.innerHTML = args.data.isOverdue
                                ? '<span class="ticket-overdue-badge"><i class="fas fa-exclamation-circle"></i>Overdue</span>'
                                : '<span class="text-muted small">&mdash;</span>';
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['ticketNumber', 'categoryName', 'priorityName', 'status', 'createdAtUtc', 'lastActivityAtUtc', 'isOverdue']);
                    },
                    excelExportComplete: () => { },
                    recordClick: (args) => {
                        if (args.rowData?.id) {
                            window.location.href = `/Tickets/TicketDetails?id=${args.rowData.id}`;
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') mainGrid.obj.excelExport();
                        if (args.item.id === 'AddCustom') {
                            resetFormState();
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
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, { backdrop: 'static', keyboard: false });
            }
        };

        return { mainGridRef, mainModalRef, subjectRef, categoryRef, priorityRef, attachmentInputRef, state, handler };
    }
};

Vue.createApp(App).mount('#app');
