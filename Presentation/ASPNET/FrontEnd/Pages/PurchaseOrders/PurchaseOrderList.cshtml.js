const App = {
    setup() {
        const MODAL_EXPANDED_KEY = 'purchaseOrder.modalExpanded';
        const readModalExpanded = () => {
            try {
                return localStorage.getItem(MODAL_EXPANDED_KEY) === '1';
            } catch {
                return false;
            }
        };

        const state = Vue.reactive({
            modalExpanded: readModalExpanded(),
            mainData: [],
            filterOpen: false,
            filter: {
                statusId: null,
                vendorId: null,
                taxId: null,
                fromDate: null,
                toDate: null,
            },
            deleteMode: false,
            vendorListLookupData: [],
            taxListLookupData: [],
            purchaseOrderStatusListLookupData: [],
            secondaryData: [],
            productListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            orderDate: new Date(),
            description: '',
            referenceNumber: '',
            // Vendor combo: search existing vendors by name/phone. status: idle | open | selected.
            // A vendor is master data (created via the quick-create modal), never inline on save.
            vendor: {
                input: '',
                status: 'idle',
                id: null,
                name: '',
                number: '',
                phoneNumber: '',
                suggestions: [],
                open: false,
                activeIndex: -1
            },
            productSearch: {
                term: '',
                open: false,
                activeIndex: 0
            },
            pickError: '',
            cartFlash: '',
            taxId: null,
            orderStatus: null,
            errors: {
                orderDate: '',
                vendorId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            },
            isSubmitting: false,
            isReceiving: false,
            isAddingLine: false,
            productPick: {
                productId: null,
                unitPrice: 0,
                commission: 0,
                quantity: 1
            },
            productHint: {
                name: '',
                number: ''
            },
            paymentMethodListLookupData: [],
            paymentList: [],
            paymentSummary: null,
            isFullySettled: false,
            isPaymentSubmitting: false,
            paymentError: '',
            newPayment: {
                paymentDate: '',
                paymentMethodId: '',
                amount: null,
                referenceNumber: ''
            },
            subTotalAmount: '0.00',
            discountAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00',
            amountInWords: '',
            vendorGroupListLookupData: [],
            vendorCategoryListLookupData: [],
            vendorQuickName: '',
            vendorQuickCountry: '',
            vendorQuickVendorGroupId: null,
            vendorQuickVendorCategoryId: null,
            vendorQuickStreet: '',
            vendorQuickCity: '',
            vendorQuickState: '',
            vendorQuickZipCode: '',
            vendorQuickPhoneNumber: '',
            vendorQuickEmailAddress: '',
            vendorQuickIsSubmitting: false,
            vendorQuickErrors: {
                name: '',
                vendorGroupId: '',
                vendorCategoryId: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                phoneNumber: '',
                emailAddress: '',
            },
            vendorGroupQuickName: '',
            vendorGroupQuickDescription: '',
            vendorGroupQuickIsSubmitting: false,
            vendorGroupQuickErrors: { name: '' },
            vendorCategoryQuickName: '',
            vendorCategoryQuickDescription: '',
            vendorCategoryQuickIsSubmitting: false,
            vendorCategoryQuickErrors: { name: '' },
            taxQuickName: '',
            taxQuickPercentage: null,
            taxQuickDescription: '',
            taxQuickIsSubmitting: false,
            taxQuickErrors: { name: '', percentage: '' },
            view: {
                id: '', number: '', orderDate: '', orderStatusName: '', statusClass: '',
                description: '', referenceNumber: '', vendorName: '', taxName: '',
                subTotalAmount: 0, discountAmount: 0, beforeTaxAmount: 0, taxAmount: 0, afterTaxAmount: 0,
                items: []
            }
        });

        const mainGridRef = Vue.ref(null);
        const filterPanelRef = Vue.ref(null);
        const filterStatusRef = Vue.ref(null);
        const filterVendorRef = Vue.ref(null);
        const filterTaxRef = Vue.ref(null);
        const filterFromDateRef = Vue.ref(null);
        const filterToDateRef = Vue.ref(null);
        const paymentDateRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const vendorInputRef = Vue.ref(null);
        const productSearchRef = Vue.ref(null);
        const pickQtyRef = Vue.ref(null);
        const pickPriceRef = Vue.ref(null);
        const paymentAmountRef = Vue.ref(null);
        const saveButtonRef = Vue.ref(null);
        const vendorQuickModalRef = Vue.ref(null);
        const vendorQuickGroupIdRef = Vue.ref(null);
        const vendorQuickCategoryIdRef = Vue.ref(null);
        const vendorGroupQuickModalRef = Vue.ref(null);
        const vendorCategoryQuickModalRef = Vue.ref(null);
        const taxQuickModalRef = Vue.ref(null);
        const viewModalRef = Vue.ref(null);

        // Running net line total for the "Add Products" form (unit cost less commission %).
        const posLineTotal = Vue.computed(() =>
            (state.productPick.unitPrice || 0) * (1 - Math.min(100, Math.max(0, state.productPick.commission || 0)) / 100) * (state.productPick.quantity || 0));

        const formatQty = (value) => Number(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });

        // ── List filtering (client-side over the already-fetched list; no API change) ──────────
        const startOfDay = (d) => { const x = new Date(d); x.setHours(0, 0, 0, 0); return x; };
        const endOfDay = (d) => { const x = new Date(d); x.setHours(23, 59, 59, 999); return x; };

        const getFilteredData = () => {
            let data = state.mainData || [];
            const f = state.filter;
            if (f.statusId !== null && f.statusId !== undefined && f.statusId !== '') {
                data = data.filter(x => Number(x.orderStatus) === Number(f.statusId));
            }
            if (f.vendorId) data = data.filter(x => x.vendorId === f.vendorId);
            if (f.taxId) data = data.filter(x => x.taxId === f.taxId);
            if (f.fromDate) {
                const from = startOfDay(f.fromDate);
                data = data.filter(x => x.createdAtUtc instanceof Date && x.createdAtUtc >= from);
            }
            if (f.toDate) {
                const to = endOfDay(f.toDate);
                data = data.filter(x => x.createdAtUtc instanceof Date && x.createdAtUtc <= to);
            }
            return data;
        };

        const activeFilterCount = Vue.computed(() => {
            const f = state.filter;
            let count = 0;
            if (f.statusId !== null && f.statusId !== undefined && f.statusId !== '') count++;
            if (f.vendorId) count++;
            if (f.taxId) count++;
            if (f.fromDate) count++;
            if (f.toDate) count++;
            return count;
        });

        // Re-run the filter and push the result into the grid (keeps the grid's own paging/sorting).
        const applyFilter = () => {
            if (mainGrid.obj) mainGrid.obj.setProperties({ dataSource: getFilteredData() });
        };

        // Drop the panel directly beneath the grid toolbar, spanning the grid width.
        const positionFilterPanel = () => {
            const wrap = mainGridRef.value?.closest('.po-grid-wrap');
            const toolbar = mainGridRef.value?.querySelector('.e-toolbar');
            if (!wrap || !toolbar || !filterPanelRef.value) return;
            const top = toolbar.getBoundingClientRect().bottom - wrap.getBoundingClientRect().top;
            filterPanelRef.value.style.top = `${top}px`;
        };

        const onWindowResize = () => { if (state.filterOpen) positionFilterPanel(); };

        const focusEl = (elRef, select = false) => Vue.nextTick(() => {
            const el = elRef.value;
            if (!el) return;
            el.focus();
            if (select) el.select?.();
        });

        const resetProductPick = () => {
            state.productPick = { productId: null, unitPrice: 0, commission: 0, quantity: 1 };
            state.productHint = { name: '', number: '' };
            state.productSearch.term = '';
            state.productSearch.open = false;
            state.productSearch.activeIndex = 0;
            state.pickError = '';
        };

        // ── Vendor: search combo over the already-loaded vendor list ───────────────────────────
        const emptyVendor = () => ({
            input: '', status: 'idle', id: null, name: '', number: '', phoneNumber: '',
            suggestions: [], open: false, activeIndex: -1
        });

        const resetVendor = () => {
            state.vendor = emptyVendor();
        };

        const setVendor = (v) => {
            state.vendor = {
                ...emptyVendor(),
                input: v.name ?? '',
                status: 'selected',
                id: v.id,
                name: v.name ?? '',
                number: v.number ?? '',
                phoneNumber: v.phoneNumber ?? ''
            };
            state.errors.vendorId = '';
        };

        // Resolve a vendor id back to its record for display (edit/view). Falls back to the name
        // carried on the list row if the vendor list has not finished loading yet.
        const loadVendor = (vendorId, fallbackName) => {
            resetVendor();
            if (!vendorId) return;
            const v = (state.vendorListLookupData ?? []).find(x => x.id === vendorId);
            setVendor(v ?? { id: vendorId, name: fallbackName ?? '', number: '', phoneNumber: '' });
        };

        const filterVendors = (term) => {
            const t = (term ?? '').trim().toLowerCase();
            const src = state.vendorListLookupData ?? [];
            const matched = t
                ? src.filter(v => (v.name || '').toLowerCase().includes(t) || (v.phoneNumber || '').toLowerCase().includes(t))
                : src;
            return matched.slice(0, 8);
        };

        const vendorField = {
            onInput: () => {
                const c = state.vendor;
                c.id = null;
                c.name = '';
                c.number = '';
                c.phoneNumber = '';
                c.status = 'idle';
                state.errors.vendorId = '';
                c.suggestions = filterVendors(c.input);
                c.open = c.suggestions.length > 0;
                c.activeIndex = c.suggestions.length ? 0 : -1;
            },
            select: (v) => {
                setVendor(v);
                focusEl(productSearchRef);
            },
            onKeydown: (e) => {
                const c = state.vendor;
                if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                    if (!c.suggestions.length) return;
                    e.preventDefault();
                    if (!c.open) {
                        c.open = true;
                        c.activeIndex = Math.max(0, c.activeIndex);
                        return;
                    }
                    const delta = e.key === 'ArrowDown' ? 1 : -1;
                    c.activeIndex = (c.activeIndex + delta + c.suggestions.length) % c.suggestions.length;
                } else if (e.key === 'Escape') {
                    if (c.open) {
                        e.preventDefault();
                        e.stopPropagation();
                        c.open = false;
                    }
                } else if (e.key === 'Tab') {
                    c.open = false;
                } else if (e.key === 'Enter' && !e.ctrlKey && !e.metaKey) {
                    e.preventDefault();
                    if (c.open && c.suggestions[c.activeIndex]) {
                        vendorField.select(c.suggestions[c.activeIndex]);
                    } else if (c.suggestions.length === 1) {
                        vendorField.select(c.suggestions[0]);
                    } else if (c.id) {
                        c.open = false;
                        focusEl(productSearchRef);
                    } else {
                        state.errors.vendorId = 'Pick a vendor from the list, or add a new one.';
                    }
                }
            },
            onFocus: () => {
                const c = state.vendor;
                if (!c.id) {
                    c.suggestions = filterVendors(c.input);
                    c.open = c.suggestions.length > 0;
                    if (c.activeIndex < 0 && c.suggestions.length) c.activeIndex = 0;
                }
            },
            // Suggestion clicks use mousedown.prevent, so blur here is a real focus change.
            onBlur: () => {
                state.vendor.open = false;
            },
            clear: () => {
                resetVendor();
                focusEl(vendorInputRef);
            }
        };

        // ── Product search (name / SKU / barcode; also the scanner target) ──────────────────
        const productMatches = Vue.computed(() => {
            const term = state.productSearch.term.trim().toLowerCase();
            if (!term || state.productPick.productId) return [];
            const scored = [];
            for (const p of state.productListLookupData ?? []) {
                const name = (p.name || '').toLowerCase();
                const number = (p.number || '').toLowerCase();
                const barcode = (p.barcode || '').toLowerCase();
                let score = -1;
                if (barcode === term || number === term) score = 0;
                else if (name.startsWith(term)) score = 1;
                else if (name.includes(term)) score = 2;
                else if (number.includes(term) || (barcode && barcode.includes(term))) score = 3;
                if (score >= 0) scored.push({ p, score });
            }
            return scored
                .sort((a, b) => a.score - b.score || (a.p.name || '').localeCompare(b.p.name || ''))
                .slice(0, 8)
                .map(x => x.p);
        });

        const findExactProduct = (term) => {
            const lower = term.trim().toLowerCase();
            if (!lower) return null;
            return (state.productListLookupData ?? []).find(p =>
                (p.barcode && p.barcode.toLowerCase() === lower) || (p.number && p.number.toLowerCase() === lower)) ?? null;
        };

        let cartFlashTimer = null;
        const flashCart = (message) => {
            clearTimeout(cartFlashTimer);
            state.cartFlash = message;
            cartFlashTimer = setTimeout(() => { state.cartFlash = ''; }, 2500);
        };

        // Empty product search + Enter: the cart is done, go to payment (or Save when there is none).
        const focusCheckout = () => {
            if (!state.secondaryData.length) {
                state.pickError = 'Search for a product to add it to the cart.';
                return;
            }
            if (paymentAmountRef.value) focusEl(paymentAmountRef, true);
            else focusEl(saveButtonRef);
        };

        const validateForm = function () {
            state.errors.orderDate = '';
            state.errors.vendorId = '';
            state.errors.taxId = '';
            state.errors.orderStatus = '';

            let isValid = true;

            if (!state.orderDate) {
                state.errors.orderDate = 'Order date is required.';
                isValid = false;
            }
            if (!state.vendor.id) {
                state.errors.vendorId = 'Vendor is required.';
                isValid = false;
            }
            if (!state.taxId) {
                state.errors.taxId = 'Tax is required.';
                isValid = false;
            }
            if (!state.orderStatus) {
                state.errors.orderStatus = 'Order status is required.';
                isValid = false;
            }

            return isValid;
        };

        // Put the caret on the first field validateForm flagged.
        const focusFirstError = () => {
            if (state.errors.vendorId) focusEl(vendorInputRef, true);
            else if (state.errors.orderDate) orderDatePicker.focus();
            else if (state.errors.orderStatus) purchaseOrderStatusListLookup.obj?.focusIn();
            else if (state.errors.taxId) taxListLookup.obj?.focusIn();
        };

        // Names exactly which order detail is missing, for the message under the product entry.
        const missingDetailsMessage = () => {
            const missing = [];
            if (state.errors.orderDate) missing.push('order date');
            if (state.errors.orderStatus) missing.push('status');
            if (state.errors.taxId) missing.push('tax');
            return missing.length
                ? `Select the ${missing.join(', ')} in Order Details (below the cart) first.`
                : 'Complete the order details first.';
        };

        // Status and tax rarely change between orders: start each new order with the last ones used.
        const DEFAULTS_KEY = 'purchaseOrder.defaults';
        const readOrderDefaults = () => {
            try {
                return JSON.parse(localStorage.getItem(DEFAULTS_KEY) || '{}') || {};
            } catch (error) {
                return {};
            }
        };
        const rememberOrderDefaults = () => {
            try {
                localStorage.setItem(DEFAULTS_KEY, JSON.stringify({ orderStatus: state.orderStatus, taxId: state.taxId }));
            } catch (error) {
                // Storage unavailable (private mode etc.) — defaults are a convenience only.
            }
        };
        const applyOrderDefaults = () => {
            const saved = readOrderDefaults();
            const statuses = state.purchaseOrderStatusListLookupData ?? [];
            const taxes = state.taxListLookupData ?? [];
            const savedStatus = statuses.find(x => String(x.id) === String(saved.orderStatus));
            const draft = statuses.find(x => String(x.name).toLowerCase() === 'draft');
            state.orderStatus = savedStatus ? String(savedStatus.id) : (draft ? String(draft.id) : null);
            // Tax: last used on this device → tax of the most recent purchase order → the only tax.
            const savedTax = taxes.find(x => x.id === saved.taxId);
            const latestOrder = [...(state.mainData ?? [])].sort((a, b) => (b.createdAtUtc ?? 0) - (a.createdAtUtc ?? 0))[0];
            const recentTax = latestOrder && taxes.find(x => x.id === latestOrder.taxId);
            state.taxId = savedTax?.id ?? recentTax?.id ?? (taxes.length === 1 ? taxes[0].id : null);
        };

        const paymentDatePicker = {
            obj: null,
            // The payment form is behind a v-if, so its host element comes and goes with the
            // modal. Rebuild the picker each time Vue hands back a fresh node.
            mount: (element) => {
                if (paymentDatePicker.obj) {
                    paymentDatePicker.obj.destroy();
                    paymentDatePicker.obj = null;
                }
                if (!element) return;

                paymentDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'Select date',
                    cssClass: 'e-small',
                    value: DateFormatManager.fromApiDate(state.newPayment.paymentDate),
                    change: (e) => {
                        state.newPayment.paymentDate = DateFormatManager.toApiDate(e.value) ?? '';
                    }
                });
                paymentDatePicker.obj.appendTo(element);
            },
            refresh: () => {
                if (paymentDatePicker.obj) {
                    paymentDatePicker.obj.value = DateFormatManager.fromApiDate(state.newPayment.paymentDate);
                }
            }
        };

        Vue.watch(() => paymentDateRef.value, (element) => paymentDatePicker.mount(element));

        // Pre-select the only method, or Cash, so Enter in the amount field can record the payment.
        const defaultPaymentMethodId = () => {
            const list = state.paymentMethodListLookupData ?? [];
            if (list.length === 1) return list[0].id;
            return list.find(m => String(m.name ?? '').toLowerCase().includes('cash'))?.id ?? '';
        };

        const resetNewPaymentState = () => {
            state.newPayment = {
                paymentDate: DateFormatManager.toApiDate(new Date()),
                paymentMethodId: defaultPaymentMethodId(),
                amount: null,
                referenceNumber: ''
            };
            paymentDatePicker.refresh();
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.orderDate = new Date();
            state.description = '';
            state.referenceNumber = '';
            resetVendor();
            state.taxId = null;
            state.orderStatus = null;
            state.errors = {
                orderDate: '',
                vendorId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            };
            state.secondaryData = [];
            state.paymentList = [];
            state.paymentSummary = null;
            state.isFullySettled = false;
            state.paymentError = '';
            resetNewPaymentState();
            state.subTotalAmount = '0.00';
            state.discountAmount = '0.00';
            state.taxAmount = '0.00';
            state.totalAmount = '0.00';
            state.amountInWords = '';
            resetProductPick();
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseOrderSingle: async (id) => {
                try {
                    // AxiosManager.get ignores config.params, so the id must be built into the URL.
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderSingle?id=' + encodeURIComponent(id), {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (orderDate, description, referenceNumber, orderStatus, taxId, vendorId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/CreatePurchaseOrder', {
                        orderDate: DateFormatManager.toApiDate(orderDate), description, referenceNumber, orderStatus, taxId, vendorId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, orderDate, description, referenceNumber, orderStatus, taxId, vendorId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/UpdatePurchaseOrder', {
                        id, orderDate: DateFormatManager.toApiDate(orderDate), description, referenceNumber, orderStatus, taxId, vendorId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/DeletePurchaseOrder', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            receiveNowMainData: async (purchaseOrderId, receiveDate, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrder/ReceiveNow', {
                        purchaseOrderId, receiveDate: DateFormatManager.toApiDate(receiveDate), description, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Vendor/GetVendorList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getTaxListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Tax/GetTaxList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseOrderStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSecondaryData: async (purchaseOrderId) => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrderItem/GetPurchaseOrderItemByPurchaseOrderIdList?purchaseOrderId=' + purchaseOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createSecondaryData: async (unitPrice, commissionRate, quantity, remark, productId, purchaseOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/CreatePurchaseOrderItem', {
                        unitPrice, commissionRate, quantity, remark, productId, purchaseOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, unitPrice, commissionRate, quantity, remark, productId, purchaseOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/UpdatePurchaseOrderItem', {
                        id, unitPrice, commissionRate, quantity, remark, productId, purchaseOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PurchaseOrderItem/DeletePurchaseOrderItem', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Product/GetProductList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductByBarcode: async (barcode) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    const response = await AxiosManager.get(`/Product/GetProductByBarcode?barcode=${encodeURIComponent(barcode)}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentMethodListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentMethod/GetPaymentMethodList?activeOnly=true', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentListByModule: async (purchaseOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentListByModule?moduleName=PurchaseOrder&moduleId=${purchaseOrderId}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentSummaryData: async (purchaseOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentSummary?moduleName=PurchaseOrder&moduleId=${purchaseOrderId}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createPayment: async (payload) => {
                try {
                    const response = await AxiosManager.post('/Payment/CreatePayment', payload);
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deletePaymentData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Payment/DeletePayment', { id, deletedById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createVendor: async (name, vendorGroupId, vendorCategoryId, street, city, state, zipCode, country, phoneNumber, emailAddress, createdById) => {
                try {
                    const response = await AxiosManager.post('/Vendor/CreateVendor', {
                        name, vendorGroupId, vendorCategoryId, street, city, state, zipCode, country,
                        phoneNumber, emailAddress, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorGroupListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/VendorGroup/GetVendorGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getVendorCategoryListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/VendorCategory/GetVendorCategoryList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createVendorGroup: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/VendorGroup/CreateVendorGroup', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createVendorCategory: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/VendorCategory/CreateVendorCategory', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createTax: async (name, percentage, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/Tax/CreateTax', { name, percentage, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateVendorListLookupData: async () => {
                const response = await services.getVendorListLookupData();
                state.vendorListLookupData = response?.data?.content?.data ?? [];
            },
            populateTaxListLookupData: async () => {
                const response = await services.getTaxListLookupData();
                state.taxListLookupData = response?.data?.content?.data;
            },
            populatePurchaseOrderStatusListLookupData: async () => {
                const response = await services.getPurchaseOrderStatusListLookupData();
                state.purchaseOrderStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSecondaryData: async (purchaseOrderId) => {
                try {
                    const response = await services.getSecondaryData(purchaseOrderId);
                    state.secondaryData = response?.data?.content?.data.map(item => ({
                        ...item,
                        createdAtUtc: new Date(item.createdAtUtc)
                    }));
                    methods.refreshPaymentSummary(purchaseOrderId);
                } catch (error) {
                    state.secondaryData = [];
                }
            },
            populateProductListLookupData: async () => {
                const response = await services.getProductListLookupData();
                state.productListLookupData = response?.data?.content?.data ?? [];
            },
            populatePaymentMethodListLookupData: async () => {
                const response = await services.getPaymentMethodListLookupData();
                state.paymentMethodListLookupData = response?.data?.content?.data ?? [];
                if (!state.newPayment.paymentMethodId) state.newPayment.paymentMethodId = defaultPaymentMethodId();
            },
            populateVendorGroupListLookupData: async () => {
                const response = await services.getVendorGroupListLookupData();
                state.vendorGroupListLookupData = response?.data?.content?.data ?? [];
                if (vendorGroupListLookupQuick.obj) {
                    vendorGroupListLookupQuick.obj.setProperties({ dataSource: state.vendorGroupListLookupData });
                }
            },
            populateVendorCategoryListLookupData: async () => {
                const response = await services.getVendorCategoryListLookupData();
                state.vendorCategoryListLookupData = response?.data?.content?.data ?? [];
                if (vendorCategoryListLookupQuick.obj) {
                    vendorCategoryListLookupQuick.obj.setProperties({ dataSource: state.vendorCategoryListLookupData });
                }
            },
            refreshPaymentSummary: async (id) => {
                const record = state.mainData.find(item => item.id === id);
                if (record) {
                    state.subTotalAmount = NumberFormatManager.formatToLocale(record.subTotalAmount ?? 0);
                    state.discountAmount = NumberFormatManager.formatToLocale(record.discountAmount ?? 0);
                    state.taxAmount = NumberFormatManager.formatToLocale(record.taxAmount ?? 0);
                    state.totalAmount = NumberFormatManager.formatToLocale(record.afterTaxAmount ?? 0);
                    state.amountInWords = AmountInWordsManager.convert(record.afterTaxAmount ?? 0);
                }

                await methods.refreshPaymentLedger(id);
            },
            refreshPaymentLedger: async (id) => {
                if (!id) {
                    state.paymentList = [];
                    state.paymentSummary = null;
                    state.isFullySettled = false;
                    return;
                }

                try {
                    const [listResponse, summaryResponse] = await Promise.all([
                        services.getPaymentListByModule(id),
                        services.getPaymentSummaryData(id)
                    ]);

                    state.paymentList = listResponse?.data?.content?.data ?? [];
                    state.paymentSummary = summaryResponse?.data?.content?.data ?? null;
                    state.isFullySettled = state.paymentSummary?.isFullySettled ?? false;
                } catch (error) {
                    state.paymentList = [];
                    state.paymentSummary = null;
                    state.isFullySettled = false;
                }
            },
            handleFormSubmit: async () => {
                // Rapid Enter / double-clicks: one save at a time.
                if (state.isSubmitting) return;
                state.isSubmitting = true;

                try {
                    if (state.deleteMode) {
                        const response = await services.deleteMainData(state.id, StorageManager.getUserId());
                        if (response.data.code === 200) {
                            await methods.populateMainData();
                            mainGrid.refresh();
                            notify('success', 'Purchase order deleted');
                            mainModal.obj.hide();
                            resetFormState();
                        } else {
                            Swal.fire({ icon: 'error', title: 'Delete Failed', text: response.data.message ?? 'Please try again.' });
                        }
                        return;
                    }

                    if (!state.id) {
                        // New order: the same path the first "Add" uses, so both share one in-flight create.
                        const result = await ensureHeaderSaved();
                        if (result.ok) {
                            notify('success', `Purchase order ${state.number} created`);
                            focusEl(productSearchRef);
                        } else if (result.message) {
                            Swal.fire({ icon: 'error', title: 'Save Failed', text: result.message });
                        }
                        return;
                    }

                    if (!validateForm()) {
                        focusFirstError();
                        return;
                    }

                    const response = await services.updateMainData(
                        state.id, state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendor.id, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        applySavedHeader(response.data.content.data);
                        await methods.populateMainData();
                        mainGrid.refresh();
                        await methods.refreshPaymentSummary(state.id);
                        notify('success', 'Order saved');
                    } else {
                        Swal.fire({ icon: 'error', title: 'Save Failed', text: response.data.message ?? 'Please check your data.' });
                    }
                } catch (error) {
                    // Entered data stays on screen; only the message is shown.
                    Swal.fire({
                        icon: 'error',
                        title: state.deleteMode ? 'Delete Failed' : 'Save Failed',
                        text: error.response?.data?.message ?? 'Please try again.'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            receiveNow: async () => {
                if (state.isReceiving || state.isSubmitting) return;
                if (state.deleteMode) return;

                if (!validateForm()) {
                    focusFirstError();
                    return;
                }

                state.isReceiving = true;
                let savedNumber = '';

                try {
                    // 1. Persist the purchase order (reusing the normal save paths).
                    if (!state.id) {
                        const header = await ensureHeaderSaved();
                        if (!header.ok) {
                            if (header.message) {
                                Swal.fire({ icon: 'error', title: 'Save Failed', text: header.message });
                            } else {
                                focusFirstError();
                            }
                            return;
                        }
                    } else {
                        const saveResponse = await services.updateMainData(
                            state.id, state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendor.id, StorageManager.getUserId());

                        if (saveResponse.data.code !== 200) {
                            Swal.fire({
                                icon: 'error',
                                title: 'Save Failed',
                                text: saveResponse.data.message ?? 'Please check your data.'
                            });
                            return;
                        }
                        applySavedHeader(saveResponse.data.content.data);
                    }

                    savedNumber = state.number;

                    // 2. Receive the remaining quantities through the existing goods-receive workflow.
                    let receiveResponse;
                    try {
                        receiveResponse = await services.receiveNowMainData(
                            state.id, state.orderDate, state.description, StorageManager.getUserId());
                    } catch (error) {
                        // The order is saved; only receiving failed. Keep it and offer a retry.
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'warning',
                            title: 'Order Saved, Receiving Failed',
                            html: `Purchase order <strong>${savedNumber}</strong> was saved, but receiving is still pending.<br>`
                                + `${(error.response?.data?.message ?? 'Please try again.')}`,
                            confirmButtonText: 'OK'
                        });
                        return;
                    }

                    const result = receiveResponse?.data?.content;
                    await methods.populateMainData();
                    mainGrid.refresh();
                    await methods.refreshPaymentSummary(state.id);

                    if (receiveResponse.data.code === 200 && result?.received) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Received',
                            html: `Purchase order <strong>${savedNumber}</strong> received.<br>Goods receive: <strong>${result.goodsReceiveNumber ?? ''}</strong>`
                        });
                    } else if (result?.alreadyReceived) {
                        Swal.fire({
                            icon: 'info',
                            title: 'Already Received',
                            html: `Purchase order <strong>${savedNumber}</strong> has no outstanding quantities to receive.`
                        });
                    } else {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Order Saved, Receiving Incomplete',
                            text: receiveResponse.data.message ?? 'The order was saved but receiving did not complete.'
                        });
                    }
                } finally {
                    state.isReceiving = false;
                }
            },
            onMainModalHidden: () => {
                state.vendor.open = false;
                state.productSearch.open = false;
                state.errors.orderDate = '';
                state.errors.vendorId = '';
                state.errors.taxId = '';
                state.errors.orderStatus = '';
                taxListLookup.trackingChange = false;
            }
        };

        const taxListLookup = {
            obj: null,
            trackingChange: false,
            create: () => {
                if (state.taxListLookupData && Array.isArray(state.taxListLookupData)) {
                    taxListLookup.obj = new ej.dropdowns.DropDownList({
                        allowFiltering: true,
                        filterType: 'Contains',
                        dataSource: state.taxListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select a Tax',
                        value: state.taxId,
                        change: async (e) => {
                            state.taxId = e.value;
                            if (e.isInteracted && taxListLookup.trackingChange) {
                                await methods.handleFormSubmit();
                            }
                        }
                    });
                    taxListLookup.obj.appendTo(taxIdRef.value);
                }
            },
            refresh: () => {
                if (taxListLookup.obj) {
                    taxListLookup.obj.value = state.taxId;
                }
            }
        };

        const purchaseOrderStatusListLookup = {
            obj: null,
            create: () => {
                if (state.purchaseOrderStatusListLookupData && Array.isArray(state.purchaseOrderStatusListLookupData)) {
                    purchaseOrderStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        allowFiltering: true,
                        filterType: 'Contains',
                        dataSource: state.purchaseOrderStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select an Order Status',
                        value: state.orderStatus,
                        change: (e) => {
                            state.orderStatus = e.value;
                        }
                    });
                    purchaseOrderStatusListLookup.obj.appendTo(orderStatusRef.value);
                }
            },
            refresh: () => {
                if (purchaseOrderStatusListLookup.obj) {
                    purchaseOrderStatusListLookup.obj.value = state.orderStatus;
                }
            }
        };

        const vendorGroupListLookupQuick = {
            obj: null,
            create: () => {
                vendorGroupListLookupQuick.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.vendorGroupListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Vendor Group',
                    filterBarPlaceholder: 'Search',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        let query = new ej.data.Query();
                        if (e.text !== '') {
                            query = query.where('name', 'startsWith', e.text, true);
                        }
                        e.updateData(state.vendorGroupListLookupData, query);
                    },
                    change: (e) => {
                        state.vendorQuickVendorGroupId = e.value;
                        state.vendorQuickErrors.vendorGroupId = '';
                    }
                });
                vendorGroupListLookupQuick.obj.appendTo(vendorQuickGroupIdRef.value);
            }
        };

        const vendorCategoryListLookupQuick = {
            obj: null,
            create: () => {
                vendorCategoryListLookupQuick.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.vendorCategoryListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'Select a Vendor Category',
                    filterBarPlaceholder: 'Search',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        let query = new ej.data.Query();
                        if (e.text !== '') {
                            query = query.where('name', 'startsWith', e.text, true);
                        }
                        e.updateData(state.vendorCategoryListLookupData, query);
                    },
                    change: (e) => {
                        state.vendorQuickVendorCategoryId = e.value;
                        state.vendorQuickErrors.vendorCategoryId = '';
                    }
                });
                vendorCategoryListLookupQuick.obj.appendTo(vendorQuickCategoryIdRef.value);
            }
        };

        // ── Filter-panel controls (live filtering) ────────────────────────────────────────────
        const filterStatusDropdown = {
            obj: null,
            create: () => {
                filterStatusDropdown.obj = new ej.dropdowns.DropDownList({
                    allowFiltering: true,
                    filterType: 'Contains',
                    dataSource: state.purchaseOrderStatusListLookupData ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All statuses',
                    showClearButton: true,
                    change: (e) => { state.filter.statusId = e.value; applyFilter(); }
                });
                filterStatusDropdown.obj.appendTo(filterStatusRef.value);
            }
        };

        const filterVendorDropdown = {
            obj: null,
            create: () => {
                filterVendorDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.vendorListLookupData ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All vendors',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    showClearButton: true,
                    filterBarPlaceholder: 'Search vendor',
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        const term = (e.text || '').toLowerCase();
                        const source = state.vendorListLookupData ?? [];
                        e.updateData(term ? source.filter(v => (v.name || '').toLowerCase().includes(term)) : source);
                    },
                    change: (e) => { state.filter.vendorId = e.value; applyFilter(); }
                });
                filterVendorDropdown.obj.appendTo(filterVendorRef.value);
            }
        };

        const filterTaxDropdown = {
            obj: null,
            create: () => {
                filterTaxDropdown.obj = new ej.dropdowns.DropDownList({
                    allowFiltering: true,
                    filterType: 'Contains',
                    dataSource: state.taxListLookupData ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All taxes',
                    sortOrder: 'Ascending',
                    showClearButton: true,
                    change: (e) => { state.filter.taxId = e.value; applyFilter(); }
                });
                filterTaxDropdown.obj.appendTo(filterTaxRef.value);
            }
        };

        const filterFromDatePicker = {
            obj: null,
            create: () => {
                filterFromDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'From date',
                    showClearButton: true,
                    change: (e) => { state.filter.fromDate = e.value; applyFilter(); }
                });
                filterFromDatePicker.obj.appendTo(filterFromDateRef.value);
            }
        };

        const filterToDatePicker = {
            obj: null,
            create: () => {
                filterToDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    placeholder: 'To date',
                    showClearButton: true,
                    change: (e) => { state.filter.toDate = e.value; applyFilter(); }
                });
                filterToDatePicker.obj.appendTo(filterToDateRef.value);
            }
        };

        const createFilterControls = () => {
            filterStatusDropdown.create();
            filterVendorDropdown.create();
            filterTaxDropdown.create();
            filterFromDatePicker.create();
            filterToDatePicker.create();
        };

        const filterHandler = {
            toggle: () => {
                state.filterOpen = !state.filterOpen;
                if (state.filterOpen) Vue.nextTick(positionFilterPanel);
            },
            clear: () => {
                state.filter.statusId = null;
                state.filter.vendorId = null;
                state.filter.taxId = null;
                state.filter.fromDate = null;
                state.filter.toDate = null;
                filterStatusDropdown.obj?.setProperties({ value: null });
                filterVendorDropdown.obj?.setProperties({ value: null });
                filterTaxDropdown.obj?.setProperties({ value: null });
                filterFromDatePicker.obj?.setProperties({ value: null });
                filterToDatePicker.obj?.setProperties({ value: null });
                applyFilter();
            }
        };

        const orderDatePicker = {
            obj: null,
            create: () => {
                orderDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'dd/MM/yyyy',
                    value: state.orderDate ? new Date(state.orderDate) : null,
                    change: (e) => {
                        state.orderDate = e.value;
                    }
                });
                orderDatePicker.obj.appendTo(orderDateRef.value);
            },
            refresh: () => {
                if (orderDatePicker.obj) {
                    orderDatePicker.obj.value = state.orderDate ? new Date(state.orderDate) : null;
                }
            },
            focus: () => {
                if (!orderDatePicker.obj) return;
                orderDatePicker.obj.focusIn();
                orderDatePicker.obj.element?.select?.();
            }
        };

        const watcherStops = [];

        watcherStops.push(Vue.watch(
            () => state.orderDate,
            (newVal, oldVal) => {
                orderDatePicker.refresh();
                state.errors.orderDate = '';
            }
        ));

        watcherStops.push(Vue.watch(
            () => state.taxId,
            (newVal, oldVal) => {
                taxListLookup.refresh();
                state.errors.taxId = '';
            }
        ));

        watcherStops.push(Vue.watch(
            () => state.orderStatus,
            (newVal, oldVal) => {
                purchaseOrderStatusListLookup.refresh();
                state.errors.orderStatus = '';
            }
        ));

        const vendorQuickModal = {
            obj: null,
            create: () => {
                vendorQuickModal.obj = new bootstrap.Modal(vendorQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const vendorGroupQuickModal = {
            obj: null,
            create: () => {
                vendorGroupQuickModal.obj = new bootstrap.Modal(vendorGroupQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const vendorCategoryQuickModal = {
            obj: null,
            create: () => {
                vendorCategoryQuickModal.obj = new bootstrap.Modal(vendorCategoryQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const taxQuickModal = {
            obj: null,
            create: () => {
                taxQuickModal.obj = new bootstrap.Modal(taxQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const vendorQuickHandler = {
            resetForm: () => {
                state.vendorQuickName = '';
                state.vendorQuickCountry = '';
                state.vendorQuickVendorGroupId = null;
                state.vendorQuickVendorCategoryId = null;
                state.vendorQuickStreet = '';
                state.vendorQuickCity = '';
                state.vendorQuickState = '';
                state.vendorQuickZipCode = '';
                state.vendorQuickPhoneNumber = '';
                state.vendorQuickEmailAddress = '';
                state.vendorQuickErrors = {
                    name: '', vendorGroupId: '', vendorCategoryId: '',
                    street: '', city: '', state: '', zipCode: '',
                    phoneNumber: '', emailAddress: ''
                };
                if (vendorGroupListLookupQuick.obj) vendorGroupListLookupQuick.obj.value = null;
                if (vendorCategoryListLookupQuick.obj) vendorCategoryListLookupQuick.obj.value = null;
            },
            open: async () => {
                vendorQuickHandler.resetForm();
                // Seed the search box into the new-vendor name so a typed-but-unmatched term carries over.
                if (!state.vendor.id && state.vendor.input) state.vendorQuickName = state.vendor.input.trim();
                await methods.populateVendorGroupListLookupData();
                await methods.populateVendorCategoryListLookupData();
                vendorQuickModal.obj.show();
            },
            close: () => {
                vendorQuickModal.obj.hide();
            },
            submit: async () => {
                const errors = {
                    name: '', vendorGroupId: '', vendorCategoryId: '',
                    street: '', city: '', state: '', zipCode: '',
                    phoneNumber: '', emailAddress: ''
                };
                let isValid = true;

                if (!state.vendorQuickName) { errors.name = 'Name is required.'; isValid = false; }
                if (!state.vendorQuickPhoneNumber?.trim()) {
                    errors.phoneNumber = 'Phone Number is required.'; isValid = false;
                } else if (!/^(?:\+?88)?01[3-9]\d{8}$/.test(state.vendorQuickPhoneNumber.trim().replace(/[\s-]/g, ''))) {
                    errors.phoneNumber = 'Enter a valid phone number (e.g. 01711234567 or +8801711234567).'; isValid = false;
                }
                if (state.vendorQuickEmailAddress?.trim() && !/\S+@\S+\.\S+/.test(state.vendorQuickEmailAddress.trim())) {
                    errors.emailAddress = 'Please enter a valid email address.'; isValid = false;
                }

                state.vendorQuickErrors = errors;
                if (!isValid) return;

                try {
                    state.vendorQuickIsSubmitting = true;

                    const response = await services.createVendor(
                        state.vendorQuickName,
                        state.vendorQuickVendorGroupId,
                        state.vendorQuickVendorCategoryId,
                        state.vendorQuickStreet,
                        state.vendorQuickCity,
                        state.vendorQuickState,
                        state.vendorQuickZipCode,
                        state.vendorQuickCountry,
                        state.vendorQuickPhoneNumber,
                        state.vendorQuickEmailAddress,
                        StorageManager.getUserId()
                    );

                    if (response.data.code === 200) {
                        const newVendor = response.data.content.data;

                        // Refresh the vendor list and select the new vendor in the search combo.
                        await methods.populateVendorListLookupData();
                        filterVendorDropdown.obj?.setProperties({ dataSource: state.vendorListLookupData });
                        setVendor(newVendor);

                        vendorQuickModal.obj.hide();
                        Swal.fire({
                            icon: 'success',
                            title: 'Vendor Created',
                            text: `"${newVendor.name}" has been created and selected.`,
                            timer: 1000,
                            showConfirmButton: false
                        });
                        focusEl(productSearchRef);
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Create Failed',
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
                    state.vendorQuickIsSubmitting = false;
                }
            }
        };

        const vendorGroupQuickHandler = {
            open: () => {
                state.vendorGroupQuickName = '';
                state.vendorGroupQuickDescription = '';
                state.vendorGroupQuickErrors = { name: '' };
                vendorQuickModal.obj.hide();
                setTimeout(() => vendorGroupQuickModal.obj.show(), 300);
            },
            close: () => {
                vendorGroupQuickModal.obj.hide();
                setTimeout(() => vendorQuickModal.obj.show(), 300);
            },
            submit: async () => {
                state.vendorGroupQuickErrors = { name: '' };
                if (!state.vendorGroupQuickName?.trim()) {
                    state.vendorGroupQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.vendorGroupQuickIsSubmitting = true;
                    const response = await services.createVendorGroup(
                        state.vendorGroupQuickName.trim(),
                        state.vendorGroupQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newGroup = response.data.content.data;
                        await methods.populateVendorGroupListLookupData();
                        vendorGroupListLookupQuick.obj.setProperties({ dataSource: state.vendorGroupListLookupData, value: newGroup.id });
                        state.vendorQuickVendorGroupId = newGroup.id;
                        vendorGroupQuickModal.obj.hide();
                        setTimeout(() => vendorQuickModal.obj.show(), 300);
                    } else {
                        state.vendorGroupQuickErrors.name = response.data.message ?? 'Failed to create vendor group.';
                    }
                } catch (error) {
                    state.vendorGroupQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.vendorGroupQuickIsSubmitting = false;
                }
            }
        };

        const vendorCategoryQuickHandler = {
            open: () => {
                state.vendorCategoryQuickName = '';
                state.vendorCategoryQuickDescription = '';
                state.vendorCategoryQuickErrors = { name: '' };
                vendorQuickModal.obj.hide();
                setTimeout(() => vendorCategoryQuickModal.obj.show(), 300);
            },
            close: () => {
                vendorCategoryQuickModal.obj.hide();
                setTimeout(() => vendorQuickModal.obj.show(), 300);
            },
            submit: async () => {
                state.vendorCategoryQuickErrors = { name: '' };
                if (!state.vendorCategoryQuickName?.trim()) {
                    state.vendorCategoryQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.vendorCategoryQuickIsSubmitting = true;
                    const response = await services.createVendorCategory(
                        state.vendorCategoryQuickName.trim(),
                        state.vendorCategoryQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newCategory = response.data.content.data;
                        await methods.populateVendorCategoryListLookupData();
                        vendorCategoryListLookupQuick.obj.setProperties({ dataSource: state.vendorCategoryListLookupData, value: newCategory.id });
                        state.vendorQuickVendorCategoryId = newCategory.id;
                        vendorCategoryQuickModal.obj.hide();
                        setTimeout(() => vendorQuickModal.obj.show(), 300);
                    } else {
                        state.vendorCategoryQuickErrors.name = response.data.message ?? 'Failed to create vendor category.';
                    }
                } catch (error) {
                    state.vendorCategoryQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.vendorCategoryQuickIsSubmitting = false;
                }
            }
        };

        const taxQuickHandler = {
            open: () => {
                state.taxQuickName = '';
                state.taxQuickPercentage = null;
                state.taxQuickDescription = '';
                state.taxQuickErrors = { name: '', percentage: '' };
                taxQuickModal.obj.show();
            },
            close: () => {
                taxQuickModal.obj.hide();
            },
            submit: async () => {
                const errors = { name: '', percentage: '' };
                let isValid = true;

                if (!state.taxQuickName?.trim()) { errors.name = 'Name is required.'; isValid = false; }
                if (state.taxQuickPercentage === null || state.taxQuickPercentage === '' || isNaN(Number(state.taxQuickPercentage))) {
                    errors.percentage = 'Percentage is required.';
                    isValid = false;
                }

                state.taxQuickErrors = errors;
                if (!isValid) return;

                try {
                    state.taxQuickIsSubmitting = true;

                    const response = await services.createTax(
                        state.taxQuickName.trim(),
                        Number(state.taxQuickPercentage),
                        state.taxQuickDescription,
                        StorageManager.getUserId()
                    );

                    if (response.data.code === 200) {
                        const newTax = response.data.content.data;

                        await methods.populateTaxListLookupData();
                        taxListLookup.obj.setProperties({
                            dataSource: state.taxListLookupData,
                            value: newTax.id
                        });
                        state.taxId = newTax.id;

                        taxQuickModal.obj.hide();
                        Swal.fire({
                            icon: 'success',
                            title: 'Tax Created',
                            text: `"${newTax.name}" has been created and selected.`,
                            timer: 1000,
                            showConfirmButton: false
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Create Failed',
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
                    state.taxQuickIsSubmitting = false;
                }
            }
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
                    groupSettings: {},
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 20, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'number', headerText: 'Number', width: 150, minWidth: 150 },
                        { field: 'orderDate', headerText: 'PO Date', width: 150, format: 'dd/MM/yyyy' },
                        { field: 'vendorName', headerText: 'Vendor', width: 200, minWidth: 200 },
                        { field: 'orderStatusName', headerText: 'Status', width: 150, minWidth: 150 },
                        { field: 'totalOrderedQty', headerText: 'Ordered Qty', width: 110, format: 'N2', textAlign: 'Right', type: 'number' },
                        { field: 'totalReceivedQty', headerText: 'Received Qty', width: 120, format: 'N2', textAlign: 'Right', type: 'number' },
                        {
                            field: 'receiveStatus', headerText: 'Receive Status', width: 130, minWidth: 130,
                            template: '<span class="badge" style="font-size:.75rem;font-weight:600;" v-bind:class="{"bg-success": data.receiveStatus === "Complete", "bg-warning text-dark": data.receiveStatus === "Partial", "bg-secondary": data.receiveStatus === "Not Started" || data.receiveStatus === "N/A"}">${receiveStatus}</span>'
                        },
                        { field: 'taxName', headerText: 'Tax', width: 150, minWidth: 150 },
                        { field: 'afterTaxAmount', headerText: 'Total Amount', width: 150, minWidth: 150, format: 'N2', textAlign: 'Right' },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'dd/MM/yyyy HH:mm' },
                        { field: 'createdByName', headerText: 'Created By', width: 150, minWidth: 150 }
                    ],
                    toolbar: [
                        'ExcelExport',
                        { text: 'Filter', tooltipText: 'Show / hide filters', prefixIcon: 'e-filter', id: 'FilterCustom' },
                        'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                        { text: 'Print PDF', tooltipText: 'Print PDF', id: 'PrintPDFCustom' },
                    ],
                    created: () => {
                        const searchBar = document.getElementById('MainGrid_searchbar');
                        if (searchBar) {
                            searchBar.addEventListener('input', function () {
                                mainGrid.obj.search(this.value);
                            });
                        }
                    },
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                    },
                    queryCellInfo: (args) => {
                        if (args.column.field === 'number') {
                            args.cell.style.cursor = 'pointer';
                            args.cell.style.color = 'var(--primary)';
                            args.cell.style.fontWeight = '600';
                            args.cell.onclick = () => {
                                openForRecord(args.rowData);
                            };
                        }
                    },
                    excelExportComplete: () => { },
                    recordDoubleClick: (args) => {
                        openForRecord(args.rowData);
                    },
                    rowSelected: () => {
                        const enable = mainGrid.obj.getSelectedRecords().length === 1;
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], enable);
                    },
                    rowDeselected: () => {
                        const enable = mainGrid.obj.getSelectedRecords().length === 1;
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], enable);
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            const date = new Date().toISOString().slice(0, 10);
                            mainGrid.obj.excelExport({ fileName: `PurchaseOrders_${date}.xlsx` });
                        }

                        if (args.item.id === 'FilterCustom') {
                            filterHandler.toggle();
                        }

                        if (args.item.id === 'AddCustom') {
                            openAddModal();
                        }

                        if (args.item.id === 'EditCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                await openEditForRecord(mainGrid.obj.getSelectedRecords()[0]);
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            state.paymentError = '';
                            resetNewPaymentState();
                            resetProductPick();
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Purchase Order?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.referenceNumber = selectedRecord.referenceNumber ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');
                                loadVendor(selectedRecord.vendorId, selectedRecord.vendorName);

                                await methods.populateSecondaryData(selectedRecord.id);

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/PurchaseOrders/PurchaseOrderPdf?id=' + (selectedRecord.id ?? ''), '_blank');
                            }
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
                GridHeightManager.apply(mainGrid.obj, mainGridRef.value);
            },
            refresh: () => {
                // Preserve any active filter selection when the underlying list is reloaded.
                mainGrid.obj.setProperties({ dataSource: getFilteredData() });
            }
        };

        // Non-blocking confirmation: a toast never takes focus away from the field being typed in.
        const notify = (icon, title) => Swal.fire({
            toast: true,
            position: 'top-end',
            icon,
            title,
            timer: 1800,
            showConfirmButton: false,
            returnFocus: false
        });

        // Copy a saved header back into the form.
        const applySavedHeader = (data) => {
            state.id = data.id ?? '';
            state.number = data.number ?? '';
            state.orderDate = data.orderDate ? new Date(data.orderDate) : null;
            state.description = data.description ?? '';
            state.referenceNumber = data.referenceNumber ?? '';
            state.taxId = data.taxId ?? '';
            taxListLookup.trackingChange = true;
            state.orderStatus = String(data.orderStatus ?? '');
            loadVendor(data.vendorId, data.vendor?.name);
            state.mainTitle = `Purchase Order ${state.number}`;
        };

        // Line items are saved against a header, so the first add (or Save) creates it from the
        // order fields. Concurrent callers share one request, so a double Enter can't create two orders.
        let headerSave = null;
        const ensureHeaderSaved = () => {
            if (state.id) return Promise.resolve({ ok: true });
            if (headerSave) return headerSave;

            if (!validateForm()) {
                focusFirstError();
                return Promise.resolve({ ok: false });
            }

            headerSave = (async () => {
                try {
                    const response = await services.createMainData(
                        state.orderDate, state.description, state.referenceNumber, state.orderStatus, state.taxId, state.vendor.id, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        applySavedHeader(response.data.content.data);
                        rememberOrderDefaults();
                        await methods.populateMainData();
                        mainGrid.refresh();
                        await methods.refreshPaymentLedger(state.id);
                        return { ok: true };
                    }
                    return { ok: false, message: response.data.message ?? 'Could not create the purchase order.' };
                } catch (error) {
                    const message = error.response?.data?.message ?? 'Could not create the purchase order. Please try again.';
                    return { ok: false, message };
                } finally {
                    headerSave = null;
                }
            })();
            return headerSave;
        };

        // Refresh everything that depends on the line items after a cart change.
        const refreshAfterCartChange = async () => {
            await methods.populateSecondaryData(state.id);
            await methods.populateMainData();
            mainGrid.refresh();
            await methods.refreshPaymentSummary(state.id);
        };

        const persistLine = async (line, changes) => {
            try {
                const unitPrice = changes.unitPrice ?? line.unitPrice;
                const commissionRate = changes.commissionRate ?? line.commissionRate ?? 0;
                const quantity = changes.quantity ?? line.quantity;
                await services.updateSecondaryData(
                    line.id, unitPrice, commissionRate, quantity, line.remark, line.productId, state.id, StorageManager.getUserId());
                await refreshAfterCartChange();
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'Update Failed', text: error.response?.data?.message ?? 'An error occurred.' });
                await methods.populateSecondaryData(state.id);
            }
        };

        const productField = {
            onInput: () => {
                // Typing again after a pick starts a new search.
                if (state.productPick.productId) {
                    const term = state.productSearch.term;
                    resetProductPick();
                    state.productSearch.term = term;
                }
                state.pickError = '';
                state.productSearch.activeIndex = 0;
                state.productSearch.open = state.productSearch.term.trim().length > 0;
            },
            onKeydown: async (e) => {
                const ps = state.productSearch;
                const list = productMatches.value;
                if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                    if (!list.length) return;
                    e.preventDefault();
                    if (!ps.open) {
                        ps.open = true;
                        return;
                    }
                    const delta = e.key === 'ArrowDown' ? 1 : -1;
                    ps.activeIndex = (ps.activeIndex + delta + list.length) % list.length;
                } else if (e.key === 'Escape') {
                    if (ps.open || ps.term || state.productPick.productId) {
                        e.preventDefault();
                        e.stopPropagation();
                        if (ps.open) ps.open = false;
                        else resetProductPick();
                    }
                } else if (e.key === 'Tab') {
                    ps.open = false;
                } else if (e.key === 'Enter' && !e.ctrlKey && !e.metaKey) {
                    e.preventDefault();
                    if (state.isAddingLine) return;
                    const term = ps.term.trim();
                    if (!term) {
                        focusCheckout();
                    } else if (state.productPick.productId) {
                        focusEl(pickQtyRef, true);
                    } else if (findExactProduct(term)) {
                        // Scanner / exact SKU: add one straight to the cart.
                        await cart.quickAdd(findExactProduct(term));
                    } else if (list.length) {
                        productField.pick(list[Math.min(ps.activeIndex, list.length - 1)]);
                    } else {
                        await productField.lookupBarcode(term);
                    }
                }
            },
            onBlur: () => {
                state.productSearch.open = false;
            },
            pick: (product) => {
                state.productPick = {
                    productId: product.id,
                    unitPrice: product.unitPrice ?? 0,
                    commission: 0,
                    quantity: 1
                };
                state.productHint = { name: product.name, number: product.number ?? '' };
                state.productSearch.term = product.name ?? '';
                state.productSearch.open = false;
                state.pickError = '';
                focusEl(pickQtyRef, true);
            },
            // Not in the loaded list by name/SKU/barcode: ask the server (barcode lookup).
            lookupBarcode: async (term) => {
                try {
                    const response = await services.getProductByBarcode(term);
                    const found = response?.data?.content?.data;
                    const product = found && state.productListLookupData.find(p => p.id === found.id);
                    if (product) {
                        await cart.quickAdd(product);
                        return;
                    }
                } catch (error) {
                    // Falls through to the "no match" message.
                }
                state.pickError = `No product matches "${term}".`;
            }
        };

        const cart = {
            // Validated line → (header if needed) → server. Returns true when the line was saved.
            createLine: async (product, unitPrice, commissionRate, quantity, focusOnError) => {
                state.isAddingLine = true;
                try {
                    const header = await ensureHeaderSaved();
                    if (!header.ok) {
                        state.pickError = header.message
                            ?? (state.errors.vendorId ? 'Select a vendor first.' : missingDetailsMessage());
                        return false;
                    }

                    await services.createSecondaryData(unitPrice, commissionRate, quantity, null, product.id, state.id, StorageManager.getUserId());
                    await refreshAfterCartChange();

                    resetProductPick();
                    flashCart(`Added ${product.name}`);
                    focusEl(productSearchRef);
                    return true;
                } catch (error) {
                    state.pickError = error.response?.data?.message ?? 'Could not add the item. Please try again.';
                    focusEl(focusOnError, true);
                    return false;
                } finally {
                    state.isAddingLine = false;
                }
            },
            addLine: async () => {
                if (state.isAddingLine) return;
                state.pickError = '';

                const product = state.productListLookupData.find(p => p.id === state.productPick.productId);
                if (!product) {
                    state.pickError = 'Search for a product and pick it first.';
                    focusEl(productSearchRef);
                    return;
                }

                const quantity = Number(state.productPick.quantity);
                const unitPrice = Number(state.productPick.unitPrice);
                const commissionRate = Number(state.productPick.commission) || 0;

                if (!quantity || quantity <= 0) {
                    state.pickError = 'Enter a quantity greater than zero.';
                    focusEl(pickQtyRef, true);
                    return;
                }
                if (isNaN(unitPrice) || unitPrice < 0) {
                    state.pickError = 'Enter a valid unit cost.';
                    focusEl(pickPriceRef, true);
                    return;
                }
                if (commissionRate < 0 || commissionRate > 100) {
                    state.pickError = 'Commission % must be between 0 and 100.';
                    return;
                }

                await cart.createLine(product, unitPrice, commissionRate, quantity, pickQtyRef);
            },
            // Scan / exact-SKU path: one unit at the product's cost, no further input needed.
            quickAdd: async (product) => {
                if (state.isAddingLine) return;
                state.pickError = '';
                const unitPrice = product.unitPrice ?? 0;
                const added = await cart.createLine(product, unitPrice, 0, 1, productSearchRef);
                if (!added) state.productSearch.term = '';
            },
            stepLineQty: (line, delta) => {
                const next = Math.max(0, (Number(line.quantity) || 0) + delta);
                cart.commitLineQty(line, next);
            },
            commitLineQty: async (line, value) => {
                const quantity = Number(value);
                if (!quantity || quantity <= 0) {
                    Swal.fire({ icon: 'warning', title: 'Quantity must be greater than zero' });
                    await methods.populateSecondaryData(state.id);
                    return;
                }
                await persistLine(line, { quantity });
            },
            commitLinePrice: async (line, value) => {
                const unitPrice = Number(value);
                if (isNaN(unitPrice) || unitPrice < 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit cost' });
                    await methods.populateSecondaryData(state.id);
                    return;
                }
                await persistLine(line, { unitPrice });
            },
            commitLineCommission: async (line, value) => {
                const commissionRate = Number(value);
                if (isNaN(commissionRate) || commissionRate < 0 || commissionRate > 100) {
                    Swal.fire({ icon: 'warning', title: 'Commission % must be between 0 and 100' });
                    await methods.populateSecondaryData(state.id);
                    return;
                }
                await persistLine(line, { commissionRate });
            },
            // Routine and easy to redo, so no confirmation; Clear all still asks.
            removeLine: async (line) => {
                try {
                    await services.deleteSecondaryData(line.id, StorageManager.getUserId());
                    await refreshAfterCartChange();
                    flashCart(`Removed ${line.productName ?? 'item'}`);
                    focusEl(productSearchRef);
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Delete Failed', text: error.response?.data?.message ?? 'An error occurred.' });
                }
            },
            clearAll: async () => {
                if (!state.secondaryData.length) return;

                const confirmed = await Swal.fire({
                    icon: 'warning',
                    title: 'Clear all items?',
                    text: `This removes all ${state.secondaryData.length} line item(s) from this order.`,
                    showCancelButton: true,
                    confirmButtonText: 'Clear All',
                    confirmButtonColor: '#dc3545'
                });
                if (!confirmed.isConfirmed) return;

                try {
                    const userId = StorageManager.getUserId();
                    const results = await Promise.allSettled(
                        [...state.secondaryData].map(line => services.deleteSecondaryData(line.id, userId))
                    );
                    const failed = results.filter(r => r.status === 'rejected');
                    if (failed.length > 0) {
                        await methods.populateSecondaryData(state.id);
                        Swal.fire({ icon: 'error', title: 'Error', text: `${failed.length} item(s) could not be removed.` });
                    } else {
                        await refreshAfterCartChange();
                        notify('success', 'Cart cleared');
                        focusEl(productSearchRef);
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Some items could not be removed.' });
                    await methods.populateSecondaryData(state.id);
                }
            }
        };

        // Ctrl/Cmd+Enter anywhere in the order modal saves; plain Enter is handled per field.
        const onModalKeydown = (e) => {
            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                e.preventDefault();
                methods.handleFormSubmit();
            }
        };

        // Expand/restore the order modal; the choice is remembered for the next time it opens.
        const toggleModalExpanded = () => {
            state.modalExpanded = !state.modalExpanded;
            try {
                localStorage.setItem(MODAL_EXPANDED_KEY, state.modalExpanded ? '1' : '0');
            } catch {
                // storage unavailable (private mode) — the toggle still works for this session
            }
        };

        const onMainModalShown = () => {
            if (state.deleteMode) return;
            if (!state.vendor.id) focusEl(vendorInputRef);
            else focusEl(productSearchRef);
        };

        const openAddModal = () => {
            state.deleteMode = false;
            state.mainTitle = 'New Purchase Order';
            resetFormState();
            applyOrderDefaults();
            mainModal.obj.show();
        };

        // Load a list record into the order modal for editing. Shared by the Edit toolbar button
        // and the View modal's "Edit Order" button.
        const openEditForRecord = async (record) => {
            if (!record) return;
            state.deleteMode = false;
            state.paymentError = '';
            resetNewPaymentState();
            resetProductPick();
            state.mainTitle = `Purchase Order ${record.number ?? ''}`;
            state.id = record.id ?? '';
            state.number = record.number ?? '';
            state.orderDate = record.orderDate ? new Date(record.orderDate) : null;
            state.description = record.description ?? '';
            state.referenceNumber = record.referenceNumber ?? '';
            state.taxId = record.taxId ?? '';
            taxListLookup.trackingChange = true;
            state.orderStatus = String(record.orderStatus ?? '');
            loadVendor(record.vendorId, record.vendorName);

            await methods.populateSecondaryData(record.id);

            mainModal.obj.show();
        };

        // Double-click a row / click the PO number: Draft orders are still being worked on, so
        // they open straight into Edit; Confirmed/Cancelled/etc. open the read-only View (which
        // carries its own "Edit Order" button).
        const openForRecord = (record) => {
            if (!record?.id) return;
            if (String(record.orderStatusName ?? '').toLowerCase() === 'draft') {
                openEditForRecord(record);
            } else {
                handlerRef.openViewModal(record.id);
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
                mainModalRef.value.addEventListener('shown.bs.modal', onMainModalShown);
            }
        };

        const viewModal = {
            obj: null,
            create: () => {
                viewModal.obj = new bootstrap.Modal(viewModalRef.value);
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PurchaseOrders']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                // Reflect the active-filter count on the toolbar Filter button (primary tint + badge).
                watcherStops.push(Vue.watch(activeFilterCount, (count) => {
                    const el = document.getElementById('FilterCustom');
                    if (!el) return;
                    el.classList.toggle('po-filter-active', count > 0);
                    el.setAttribute('data-filter-count', count);
                }));
                window.addEventListener('resize', onWindowResize);

                mainModal.create();
                viewModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                orderDatePicker.create();

                // Quick Shortcut "open add form" intent (/…?quickAction=add) opens the add modal on load.
                if (new URLSearchParams(window.location.search).get('quickAction') === 'add') {
                    openAddModal();
                }

                Promise.all([
                    methods.populateVendorListLookupData(),
                    methods.populateTaxListLookupData(),
                    methods.populatePurchaseOrderStatusListLookupData(),
                    methods.populateProductListLookupData(),
                    methods.populatePaymentMethodListLookupData(),
                ]).then(() => {
                    // A new order opened before the lookups arrived still gets its defaults.
                    if (!state.id && !state.deleteMode) {
                        if (!state.orderStatus || !state.taxId) {
                            const keepStatus = state.orderStatus, keepTax = state.taxId;
                            applyOrderDefaults();
                            state.orderStatus = keepStatus || state.orderStatus;
                            state.taxId = keepTax || state.taxId;
                        }
                    }
                    filterVendorDropdown.obj?.setProperties({ dataSource: state.vendorListLookupData });
                    taxListLookup.create();
                    purchaseOrderStatusListLookup.create();
                    taxQuickModal.create();
                    vendorQuickModal.create();
                    vendorGroupQuickModal.create();
                    vendorCategoryQuickModal.create();
                    vendorGroupListLookupQuick.create();
                    vendorCategoryListLookupQuick.create();
                    createFilterControls();
                });
            } catch (e) {
            } finally {

            }
        });

        Vue.onUnmounted(() => {
            clearTimeout(cartFlashTimer);
            watcherStops.forEach(stop => stop());
            window.removeEventListener('resize', onWindowResize);
            mainModalRef.value?.removeEventListener('hidden.bs.modal', methods.onMainModalHidden);
            mainModalRef.value?.removeEventListener('shown.bs.modal', onMainModalShown);
            mainGrid.obj?.destroy();
            orderDatePicker.obj?.destroy();
            taxListLookup.obj?.destroy();
            purchaseOrderStatusListLookup.obj?.destroy();
            vendorGroupListLookupQuick.obj?.destroy();
            vendorCategoryListLookupQuick.obj?.destroy();
            filterStatusDropdown.obj?.destroy();
            filterVendorDropdown.obj?.destroy();
            filterTaxDropdown.obj?.destroy();
            filterFromDatePicker.obj?.destroy();
            filterToDatePicker.obj?.destroy();
            if (paymentDateRef.value) {
                const dpInst = paymentDateRef.value?.ej2_instances?.[0];
                dpInst?.destroy();
            }
        });

        // The returned handler object, so handlers can call each other.
        let handlerRef = null;

        return {
            mainGridRef,
            filterPanelRef,
            filterStatusRef,
            filterVendorRef,
            filterTaxRef,
            filterFromDateRef,
            filterToDateRef,
            activeFilterCount,
            paymentDateRef,
            mainModalRef,
            orderDateRef,
            taxIdRef,
            orderStatusRef,
            vendorInputRef,
            productSearchRef,
            pickQtyRef,
            pickPriceRef,
            paymentAmountRef,
            saveButtonRef,
            productMatches,
            posLineTotal,
            vendorQuickModalRef,
            vendorQuickGroupIdRef,
            vendorQuickCategoryIdRef,
            vendorGroupQuickModalRef,
            vendorCategoryQuickModalRef,
            taxQuickModalRef,
            viewModalRef,
            state,
            methods,
            handler: handlerRef = {
                handleSubmit: methods.handleFormSubmit,
                receiveNow: methods.receiveNow,
                toggleFilter: filterHandler.toggle,
                clearFilters: filterHandler.clear,
                onModalKeydown: onModalKeydown,
                toggleModalExpanded: toggleModalExpanded,
                vendorInput: vendorField.onInput,
                vendorKeydown: vendorField.onKeydown,
                vendorFocus: vendorField.onFocus,
                vendorBlur: vendorField.onBlur,
                selectVendor: vendorField.select,
                clearVendor: vendorField.clear,
                productInput: productField.onInput,
                productKeydown: productField.onKeydown,
                productBlur: productField.onBlur,
                pickProduct: productField.pick,
                formatAmount: (value) => NumberFormatManager.formatToLocale(value ?? 0),
                formatQty: formatQty,
                formatDate: (value) => {
                    if (!value) return '—';
                    const d = new Date(value);
                    return isNaN(d) ? value : d.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });
                },
                stepQty: (delta) => {
                    const next = Math.max(0, (Number(state.productPick.quantity) || 0) + delta);
                    state.productPick.quantity = Number(next.toFixed(4));
                },
                clearProductPick: resetProductPick,
                addLineToCart: cart.addLine,
                stepLineQty: cart.stepLineQty,
                commitLineQty: cart.commitLineQty,
                commitLinePrice: cart.commitLinePrice,
                commitLineCommission: cart.commitLineCommission,
                removeLine: cart.removeLine,
                clearCart: cart.clearAll,
                formatPaymentDate: (value) => {
                    if (!value) return '';
                    const d = new Date(value);
                    return isNaN(d) ? '' : DateFormatManager.toApiDate(d);
                },
                payFullBalance: () => {
                    state.paymentError = '';
                    const outstanding = state.paymentSummary?.amountOutstanding ?? 0;
                    state.newPayment.amount = outstanding > 0 ? Number(outstanding.toFixed(2)) : null;
                },
                // Enter in Amount: an empty field first takes the balance due (nothing is recorded),
                // so a stray double Enter can't post a payment; Enter again records it.
                paymentAmountEnter: () => {
                    if (state.newPayment.amount === null || state.newPayment.amount === '') {
                        const outstanding = state.paymentSummary?.amountOutstanding ?? 0;
                        if (outstanding > 0) {
                            state.paymentError = '';
                            state.newPayment.amount = Number(outstanding.toFixed(2));
                            focusEl(paymentAmountRef, true);
                            return;
                        }
                    }
                    handlerRef.submitPayment();
                },
                submitPayment: async () => {
                    state.paymentError = '';

                    const amount = Number(state.newPayment.amount);

                    if (!state.newPayment.paymentDate) {
                        state.paymentError = 'Payment date is required.';
                        return;
                    }
                    if (!state.newPayment.paymentMethodId) {
                        state.paymentError = 'Select a payment method.';
                        document.getElementById('PaymentMethod')?.focus();
                        return;
                    }
                    if (!amount || amount <= 0) {
                        state.paymentError = 'Enter an amount greater than zero.';
                        focusEl(paymentAmountRef, true);
                        return;
                    }

                    const outstanding = state.paymentSummary?.amountOutstanding ?? 0;
                    if (amount > outstanding + 0.005) {
                        state.paymentError = `Amount exceeds the balance due of ${NumberFormatManager.formatToLocale(outstanding)}.`;
                        return;
                    }

                    if (state.isPaymentSubmitting) return;
                    try {
                        state.isPaymentSubmitting = true;

                        const response = await services.createPayment({
                            moduleName: 'PurchaseOrder',
                            moduleId: state.id,
                            paymentDate: state.newPayment.paymentDate,
                            amount: amount,
                            paymentMethodId: state.newPayment.paymentMethodId,
                            referenceNumber: state.newPayment.referenceNumber || null,
                            createdById: StorageManager.getUserId()
                        });

                        if (response.data.code === 200) {
                            resetNewPaymentState();
                            await methods.refreshPaymentLedger(state.id);
                            notify('success', `Payment recorded. Balance due: ${NumberFormatManager.formatToLocale(state.paymentSummary?.amountOutstanding ?? 0)}`);
                            // Settled → Save is next; otherwise stay on the amount for a split payment.
                            if (state.isFullySettled) focusEl(saveButtonRef);
                            else focusEl(paymentAmountRef, true);
                        }
                    } catch (error) {
                        state.paymentError = error.response?.data?.message ?? 'Failed to record the payment.';
                    } finally {
                        state.isPaymentSubmitting = false;
                    }
                },
                deletePayment: async (payment) => {
                    const confirmed = await Swal.fire({
                        icon: 'warning',
                        title: 'Remove this payment?',
                        html: `<strong>${NumberFormatManager.formatToLocale(payment.amount ?? 0)}</strong> via ${payment.paymentMethodName || 'unknown method'}.<br>The balance due will go back up.`,
                        showCancelButton: true,
                        confirmButtonText: 'Remove',
                        confirmButtonColor: '#dc3545'
                    });

                    if (!confirmed.isConfirmed) return;

                    try {
                        state.isPaymentSubmitting = true;
                        await services.deletePaymentData(payment.id, StorageManager.getUserId());
                        await methods.refreshPaymentLedger(state.id);
                    } catch (error) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: error.response?.data?.message ?? 'Failed to remove the payment.'
                        });
                    } finally {
                        state.isPaymentSubmitting = false;
                    }
                },
                openVendorQuickCreate: vendorQuickHandler.open,
                closeVendorQuickCreate: vendorQuickHandler.close,
                submitVendorQuickCreate: vendorQuickHandler.submit,
                openVendorGroupQuickCreate: vendorGroupQuickHandler.open,
                closeVendorGroupQuickCreate: vendorGroupQuickHandler.close,
                submitVendorGroupQuickCreate: vendorGroupQuickHandler.submit,
                openVendorCategoryQuickCreate: vendorCategoryQuickHandler.open,
                closeVendorCategoryQuickCreate: vendorCategoryQuickHandler.close,
                submitVendorCategoryQuickCreate: vendorCategoryQuickHandler.submit,
                openTaxQuickCreate: taxQuickHandler.open,
                closeTaxQuickCreate: taxQuickHandler.close,
                submitTaxQuickCreate: taxQuickHandler.submit,
                openViewModal: async (id) => {
                    try {
                        const response = await services.getPurchaseOrderSingle(id);
                        const data = response?.data?.content?.data;
                        if (!data) return;

                        const statusMap = {
                            0: 'text-bg-secondary',
                            1: 'text-bg-danger',
                            2: 'text-bg-info',
                            3: 'text-bg-dark',
                            4: 'text-bg-primary',
                            5: 'text-bg-warning',
                            6: 'text-bg-success',
                            7: 'text-bg-dark'
                        };

                        state.view = {
                            id: data.id ?? '',
                            number: data.number ?? '',
                            orderDate: data.orderDate ?? '',
                            // GetPurchaseOrderSingle returns the raw entity (no status name), so resolve
                            // it from the loaded status lookup; fall back to any name on the payload.
                            orderStatusName: (state.purchaseOrderStatusListLookupData ?? []).find(s => String(s.id) === String(data.orderStatus))?.name ?? data.orderStatusName ?? '',
                            statusClass: statusMap[data.orderStatus] ?? 'text-bg-secondary',
                            description: data.description ?? '',
                            referenceNumber: data.referenceNumber ?? '',
                            vendorName: data.vendor?.name ?? '',
                            taxName: data.tax?.name ?? '',
                            subTotalAmount: data.subTotalAmount ?? 0,
                            discountAmount: data.discountAmount ?? 0,
                            beforeTaxAmount: data.beforeTaxAmount ?? 0,
                            taxAmount: data.taxAmount ?? 0,
                            afterTaxAmount: data.afterTaxAmount ?? 0,
                            items: (data.purchaseOrderItemList ?? []).map(item => ({
                                id: item.id ?? '',
                                productName: item.product?.name ?? '',
                                unitPrice: item.unitPrice ?? 0,
                                commissionRate: item.commissionRate ?? 0,
                                quantity: item.quantity ?? 0,
                                total: item.total ?? 0
                            }))
                        };

                        viewModal.obj.show();
                    } catch (error) {
                        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to load purchase order details.' });
                    }
                },
                editFromView: () => {
                    const id = state.view.id;
                    viewModal.obj.hide();
                    if (id) {
                        const record = state.mainData.find(r => r.id === id);
                        if (record) openEditForRecord(record);
                    }
                }
            }
        };
    }
};

Vue.createApp(App).mount('#app');
