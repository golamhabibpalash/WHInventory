const App = {
    setup() {
        const state = Vue.reactive({
            ticket: null,
            history: [],
            attachments: [],
            newComment: '',
            isInternalNote: false,
            isSubmittingComment: false,
            loadError: '',
        });

        const categoryRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);
        const assigneeRef = Vue.ref(null);
        const tagsRef = Vue.ref(null);
        const attachmentInputRef = Vue.ref(null);

        const STATUS_LABELS = { 0: 'New', 1: 'Open', 2: 'In Progress', 3: 'Pending User', 4: 'Pending Internal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const STATUS_KEYS = { 0: 'New', 1: 'Open', 2: 'InProgress', 3: 'PendingUser', 4: 'PendingInternal', 5: 'Resolved', 6: 'Closed', 7: 'Reopened', 8: 'Cancelled' };
        const HISTORY_LABELS = {
            Created: 'Ticket created',
            StatusChanged: (e) => `Status changed: ${STATUS_LABELS[Object.keys(STATUS_KEYS).find(k => STATUS_KEYS[k] === e.oldValue)] ?? e.oldValue} → ${STATUS_LABELS[Object.keys(STATUS_KEYS).find(k => STATUS_KEYS[k] === e.newValue)] ?? e.newValue}`,
            PriorityChanged: 'Priority changed',
            CategoryChanged: 'Category changed',
            Assigned: (e) => `Assigned: ${e.oldValue} → ${e.newValue}`,
            CommentAdded: 'Replied',
            InternalNoteAdded: 'Added an internal note',
            AttachmentUploaded: (e) => `Uploaded attachment: ${e.newValue}`,
            AttachmentRemoved: (e) => `Removed attachment: ${e.oldValue}`,
        };

        const getTicketId = () => new URLSearchParams(window.location.search).get('id');

        const statusLabel = (status) => STATUS_LABELS[status] ?? status;
        const statusKey = (status) => STATUS_KEYS[status] ?? 'New';
        const formatDateTime = (value) => value ? DateFormatManager.formatToLocale(value) : '';
        const formatFileSize = (bytes) => {
            if (!bytes) return '0 KB';
            const kb = bytes / 1024;
            return kb < 1024 ? `${kb.toFixed(1)} KB` : `${(kb / 1024).toFixed(1)} MB`;
        };
        const historyLabel = (entry) => {
            const template = HISTORY_LABELS[entry.action];
            if (typeof template === 'function') return template(entry);
            return template || entry.action;
        };

        const isOverdue = Vue.computed(() => {
            if (!state.ticket?.slaResolutionDueAtUtc) return false;
            if ([5, 6, 8].includes(state.ticket.status)) return false;
            return new Date(state.ticket.slaResolutionDueAtUtc) < new Date();
        });

        const canResolve = Vue.computed(() => state.ticket?.currentUserIsAgent && state.ticket?.status === 2);
        const canClose = Vue.computed(() => state.ticket?.status === 5);
        const canReopen = Vue.computed(() => state.ticket?.status === 5 || state.ticket?.status === 6);

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
            getTicket: async (id) => AxiosManager.get('/Ticket/GetTicket' + toQueryString({ id })),
            getHistory: async (id) => AxiosManager.get('/Ticket/GetTicketHistory' + toQueryString({ ticketId: id })),
            getAttachments: async (id) => AxiosManager.get('/Ticket/GetTicketAttachments' + toQueryString({ ticketId: id })),
            addComment: async (payload) => AxiosManager.post('/Ticket/AddComment', payload),
            changeStatus: async (payload) => AxiosManager.post('/Ticket/ChangeTicketStatus', payload),
            resolveTicket: async (payload) => AxiosManager.post('/Ticket/ResolveTicket', payload),
            closeTicket: async (payload) => AxiosManager.post('/Ticket/CloseTicket', payload),
            reopenTicket: async (payload) => AxiosManager.post('/Ticket/ReopenTicket', payload),
            assignTicket: async (payload) => AxiosManager.post('/Ticket/AssignTicket', payload),
            changePriority: async (payload) => AxiosManager.post('/Ticket/ChangeTicketPriority', payload),
            updateTicket: async (payload) => AxiosManager.post('/Ticket/UpdateTicket', payload),
            setTags: async (payload) => AxiosManager.post('/Ticket/SetTicketTags', payload),
            uploadAttachment: async (ticketId, file) => {
                const formData = new FormData();
                formData.append('ticketId', ticketId);
                formData.append('file', file);
                return AxiosManager.post('/Ticket/UploadTicketAttachment', formData, {});
            },
            deleteAttachment: async (payload) => AxiosManager.post('/Ticket/DeleteTicketAttachment', payload),
            // A plain browser navigation/window.open to an /api/* URL carries no Authorization
            // header — this app authenticates API calls via a bearer token AxiosManager attaches,
            // not a cookie, so the download has to go through AxiosManager as a blob and be saved
            // client-side, the same way every other file fetch in this app already works (see
            // ProductList's FileImage/GetImage calls).
            downloadAttachment: async (attachmentId) => AxiosManager.get('/Ticket/DownloadTicketAttachment' + toQueryString({ attachmentId }), { responseType: 'blob' }),
            getCategoryList: async () => AxiosManager.get('/TicketCategory/GetTicketCategoryList', {}),
            getPriorityList: async () => AxiosManager.get('/TicketPriority/GetTicketPriorityList', {}),
            getTagList: async () => AxiosManager.get('/TicketTag/GetTicketTagList', {}),
            getUserList: async () => AxiosManager.get('/Security/GetUserList', {}),
        };

        const methods = {
            reloadAll: async () => {
                const id = getTicketId();
                if (!id) {
                    state.loadError = 'No ticket specified.';
                    return;
                }
                try {
                    const [ticketRes, historyRes, attachmentsRes] = await Promise.all([
                        services.getTicket(id),
                        services.getHistory(id),
                        services.getAttachments(id),
                    ]);
                    state.ticket = ticketRes.data.content;
                    state.history = historyRes.data.content.data ?? [];
                    state.attachments = attachmentsRes.data.content.data ?? [];

                    if (state.ticket.currentUserIsAgent) {
                        categoryListLookup.refresh(state.ticket.categoryId);
                        priorityListLookup.refresh(state.ticket.priorityId);
                        assigneeListLookup.refresh(state.ticket.assignedToId);
                        tagsListMultiSelect.refresh(state.ticket.tagIds);
                    }
                } catch (error) {
                    state.loadError = error.response?.data?.message ?? 'This ticket could not be loaded — it may not exist, or you may not have access to it.';
                }
            },
        };

        const handler = {
            goBack: () => { window.history.length > 1 ? window.history.back() : (window.location.href = '/Tickets/TicketList'); },

            addComment: async () => {
                if (!state.newComment) return;
                try {
                    state.isSubmittingComment = true;
                    await services.addComment({
                        ticketId: state.ticket.id,
                        message: state.newComment,
                        isInternal: state.isInternalNote,
                        createdById: StorageManager.getUserId()
                    });
                    state.newComment = '';
                    state.isInternalNote = false;
                    await methods.reloadAll();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Could not post', text: error.response?.data?.message ?? 'Please try again.' });
                } finally {
                    state.isSubmittingComment = false;
                }
            },

            resolveTicket: async () => {
                try {
                    await services.resolveTicket({ id: state.ticket.id, updatedById: StorageManager.getUserId() });
                    await methods.reloadAll();
                    Swal.fire({ icon: 'success', title: 'Ticket resolved', timer: 1200, showConfirmButton: false });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                }
            },
            closeTicket: async () => {
                try {
                    await services.closeTicket({ id: state.ticket.id, updatedById: StorageManager.getUserId() });
                    await methods.reloadAll();
                    Swal.fire({ icon: 'success', title: 'Ticket closed', timer: 1200, showConfirmButton: false });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                }
            },
            reopenTicket: async () => {
                try {
                    await services.reopenTicket({ id: state.ticket.id, updatedById: StorageManager.getUserId() });
                    await methods.reloadAll();
                    Swal.fire({ icon: 'success', title: 'Ticket reopened', timer: 1200, showConfirmButton: false });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                }
            },

            triggerAttachmentInput: () => attachmentInputRef.value?.click(),
            onAttachmentSelected: async (e) => {
                const file = e.target.files[0];
                if (!file) return;
                try {
                    await services.uploadAttachment(state.ticket.id, file);
                    await methods.reloadAll();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Upload failed', text: error.response?.data?.message ?? 'Please try again.' });
                } finally {
                    e.target.value = '';
                }
            },
            downloadAttachment: async (attachment) => {
                try {
                    const response = await services.downloadAttachment(attachment.id);
                    const url = URL.createObjectURL(response.data);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = attachment.originalName || 'attachment';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    setTimeout(() => URL.revokeObjectURL(url), 1000);
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Download failed', text: 'Please try again.' });
                }
            },
            deleteAttachment: async (attachment) => {
                const confirm = await Swal.fire({
                    icon: 'warning', title: 'Remove attachment?', text: attachment.originalName,
                    showCancelButton: true, confirmButtonText: 'Remove', cancelButtonText: 'Cancel'
                });
                if (!confirm.isConfirmed) return;
                try {
                    await services.deleteAttachment({ attachmentId: attachment.id, deletedById: StorageManager.getUserId() });
                    await methods.reloadAll();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                }
            },
        };

        const categoryListLookup = {
            obj: null,
            create: async () => {
                const response = await services.getCategoryList();
                categoryListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: (response?.data?.content?.data ?? []).filter(x => x.isActive),
                    fields: { value: 'id', text: 'name' },
                    change: async (e) => {
                        if (!e.value || e.value === state.ticket.categoryId) return;
                        try {
                            await services.updateTicket({
                                id: state.ticket.id, subject: state.ticket.subject, description: state.ticket.description,
                                categoryId: e.value, updatedById: StorageManager.getUserId()
                            });
                            await methods.reloadAll();
                        } catch (error) {
                            Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                        }
                    }
                });
                categoryListLookup.obj.appendTo(categoryRef.value);
            },
            refresh: (value) => { if (categoryListLookup.obj) categoryListLookup.obj.value = value; }
        };

        const priorityListLookup = {
            obj: null,
            create: async () => {
                const response = await services.getPriorityList();
                priorityListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: (response?.data?.content?.data ?? []).filter(x => x.isActive),
                    fields: { value: 'id', text: 'name' },
                    change: async (e) => {
                        if (!e.value || e.value === state.ticket.priorityId) return;
                        try {
                            await services.changePriority({ id: state.ticket.id, priorityId: e.value, updatedById: StorageManager.getUserId() });
                            await methods.reloadAll();
                        } catch (error) {
                            Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                        }
                    }
                });
                priorityListLookup.obj.appendTo(priorityRef.value);
            },
            refresh: (value) => { if (priorityListLookup.obj) priorityListLookup.obj.value = value; }
        };

        const assigneeListLookup = {
            obj: null,
            create: async () => {
                const response = await services.getUserList();
                assigneeListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: (response?.data?.content?.data ?? []),
                    fields: { value: 'id', text: 'email' },
                    placeholder: 'Unassigned',
                    allowClearing: true,
                    showClearButton: true,
                    change: async (e) => {
                        if (e.value === state.ticket.assignedToId) return;
                        try {
                            await services.assignTicket({ id: state.ticket.id, assignedToId: e.value || null, updatedById: StorageManager.getUserId() });
                            await methods.reloadAll();
                        } catch (error) {
                            Swal.fire({ icon: 'error', title: 'Failed', text: error.response?.data?.message ?? 'Please try again.' });
                        }
                    }
                });
                assigneeListLookup.obj.appendTo(assigneeRef.value);
            },
            refresh: (value) => { if (assigneeListLookup.obj) assigneeListLookup.obj.value = value; }
        };

        const tagsListMultiSelect = {
            obj: null,
            create: async () => {
                const response = await services.getTagList();
                tagsListMultiSelect.obj = new ej.dropdowns.MultiSelect({
                    dataSource: (response?.data?.content?.data ?? []),
                    fields: { value: 'id', text: 'name' },
                    mode: 'Box',
                    placeholder: 'Add tags...',
                    change: async (e) => {
                        try {
                            await services.setTags({ ticketId: state.ticket.id, tagIds: e.value || [], updatedById: StorageManager.getUserId() });
                        } catch (error) {
                            console.error('Failed to save tags:', error);
                        }
                    }
                });
                tagsListMultiSelect.obj.appendTo(tagsRef.value);
            },
            refresh: (value) => { if (tagsListMultiSelect.obj) tagsListMultiSelect.obj.value = value || []; }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Tickets', 'TicketAgent']);
                await SecurityManager.validateToken();

                await methods.reloadAll();

                if (state.ticket && state.ticket.currentUserIsAgent) {
                    await categoryListLookup.create();
                    await priorityListLookup.create();
                    await assigneeListLookup.create();
                    await tagsListMultiSelect.create();
                    categoryListLookup.refresh(state.ticket.categoryId);
                    priorityListLookup.refresh(state.ticket.priorityId);
                    assigneeListLookup.refresh(state.ticket.assignedToId);
                    tagsListMultiSelect.refresh(state.ticket.tagIds);
                }
            } catch (e) {
            }
        });

        return {
            state, categoryRef, priorityRef, assigneeRef, tagsRef, attachmentInputRef,
            handler, statusLabel, statusKey, formatDateTime, formatFileSize, historyLabel,
            isOverdue, canResolve, canClose, canReopen
        };
    }
};

Vue.createApp(App).mount('#app');
