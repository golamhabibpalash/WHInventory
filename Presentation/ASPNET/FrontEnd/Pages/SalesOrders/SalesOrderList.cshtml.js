const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            filterOpen: false,
            filter: {
                statusId: null,
                customerId: null,
                taxId: null,
                fromDate: null,
                toDate: null,
            },
            deleteMode: false,
            customerListLookupData: [],
            taxListLookupData: [],
            salesOrderStatusListLookupData: [],
            secondaryData: [],
            productListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            orderDate: new Date(),
            description: '',
            previousDue: 0,
            customerId: null,
            // Mobile-number customer field. status: idle | partial | searching | existing | new | invalid | error.
            // 'new' means the number is valid and unknown: the server creates the customer when the order is saved.
            customer: {
                input: '',
                status: 'idle',
                id: null,
                name: '',
                number: '',
                newName: '',
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
                customerId: '',
                taxId: '',
                orderStatus: '',
                description: ''
            },
            isSubmitting: false,
            isAddingLine: false,
            isDelivering: false,
            productPick: {
                productId: null,
                unitPrice: 0,
                commission: 0,
                quantity: 1,
                priceEdited: false
            },
            productHint: {
                productId: null,
                name: '',
                physical: false,
                stock: null,
                pendingDraft: null,
                pendingConfirmed: null,
                available: null,
                availabilityLoading: false,
                minPrice: null,
                maxPrice: null
            },
            priceSuggestion: {
                loading: false,
                sourceName: '',
                costPrice: null,
                profit: null,
                profitPercent: null
            },
            priceResolveSeq: 0,
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
            taxQuickName: '',
            taxQuickPercentage: null,
            taxQuickDescription: '',
            taxQuickIsSubmitting: false,
            taxQuickErrors: { name: '', percentage: '' },
            view: {
                id: '', number: '', orderDate: '', orderStatusName: '', statusClass: '',
                description: '', customerName: '', taxName: '',
                subTotalAmount: 0, discountAmount: 0, beforeTaxAmount: 0, taxAmount: 0, afterTaxAmount: 0,
                items: []
            }
        });

        const mainGridRef = Vue.ref(null);
        const filterPanelRef = Vue.ref(null);
        const filterStatusRef = Vue.ref(null);
        const filterCustomerRef = Vue.ref(null);
        const filterTaxRef = Vue.ref(null);
        const filterFromDateRef = Vue.ref(null);
        const filterToDateRef = Vue.ref(null);
        const paymentDateRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const orderDateRef = Vue.ref(null);
        const taxIdRef = Vue.ref(null);
        const orderStatusRef = Vue.ref(null);
        const customerInputRef = Vue.ref(null);
        const productSearchRef = Vue.ref(null);
        const pickQtyRef = Vue.ref(null);
        const pickPriceRef = Vue.ref(null);
        const paymentAmountRef = Vue.ref(null);
        const saveButtonRef = Vue.ref(null);
        const taxQuickModalRef = Vue.ref(null);
        const viewModalRef = Vue.ref(null);

        // Running net line total for the "Select Product" form (unit price less commission %).
        const posLineTotal = Vue.computed(() =>
            (state.productPick.unitPrice || 0) * (1 - Math.min(100, Math.max(0, state.productPick.commission || 0)) / 100) * (state.productPick.quantity || 0));

        // ── List filtering (client-side over the already-fetched list; no API change) ──────────
        const startOfDay = (d) => { const x = new Date(d); x.setHours(0, 0, 0, 0); return x; };
        const endOfDay = (d) => { const x = new Date(d); x.setHours(23, 59, 59, 999); return x; };

        const getFilteredData = () => {
            let data = state.mainData || [];
            const f = state.filter;
            if (f.statusId !== null && f.statusId !== undefined && f.statusId !== '') {
                data = data.filter(x => Number(x.orderStatus) === Number(f.statusId));
            }
            if (f.customerId) data = data.filter(x => x.customerId === f.customerId);
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
            if (f.customerId) count++;
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

        const filterStatusDropdown = {
            obj: null,
            create: () => {
                filterStatusDropdown.obj = new ej.dropdowns.DropDownList({
                    allowFiltering: true,
                    filterType: 'Contains',
                    dataSource: state.salesOrderStatusListLookupData ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All statuses',
                    showClearButton: true,
                    change: (e) => { state.filter.statusId = e.value; applyFilter(); }
                });
                filterStatusDropdown.obj.appendTo(filterStatusRef.value);
            }
        };

        const filterCustomerDropdown = {
            obj: null,
            create: () => {
                filterCustomerDropdown.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.customerListLookupData ?? [],
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All customers',
                    sortOrder: 'Ascending',
                    allowFiltering: true,
                    showClearButton: true,
                    filterBarPlaceholder: 'Search customer',
                    filtering: (e) => {
                        e.preventDefaultAction = true;
                        const term = (e.text || '').toLowerCase();
                        const source = state.customerListLookupData ?? [];
                        e.updateData(term ? source.filter(c => (c.name || '').toLowerCase().includes(term)) : source);
                    },
                    change: (e) => { state.filter.customerId = e.value; applyFilter(); }
                });
                filterCustomerDropdown.obj.appendTo(filterCustomerRef.value);
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
            filterCustomerDropdown.create();
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
                state.filter.customerId = null;
                state.filter.taxId = null;
                state.filter.fromDate = null;
                state.filter.toDate = null;
                filterStatusDropdown.obj?.setProperties({ value: null });
                filterCustomerDropdown.obj?.setProperties({ value: null });
                filterTaxDropdown.obj?.setProperties({ value: null });
                filterFromDatePicker.obj?.setProperties({ value: null });
                filterToDatePicker.obj?.setProperties({ value: null });
                applyFilter();
            }
        };

        const emptyProductHint = {
            productId: null, name: '', physical: false, stock: null,
            pendingDraft: null, pendingConfirmed: null, available: null, availabilityLoading: false,
            minPrice: null, maxPrice: null
        };

        // Quantity of the picked product already on this order's cart lines.
        const posInCart = Vue.computed(() =>
            state.productHint.productId ? quantityInCart(state.productHint.productId) : 0);

        // What this order can still take: on hand − pending delivery on other open orders − this cart.
        // Null until the server figures arrive (or for non-stock items).
        const posAvailable = Vue.computed(() =>
            state.productHint.physical && state.productHint.available !== null
                ? state.productHint.available - posInCart.value
                : null);

        // Warns when the picked quantity is above what can be promised. The server is the
        // authority; this only nudges the cashier before they hit "Add".
        const posStockExceeded = Vue.computed(() => {
            if (!state.productHint.physical) return false;
            const qty = state.productPick.quantity || 0;
            if (posAvailable.value !== null) return qty > posAvailable.value;
            return state.productHint.stock !== null && qty + posInCart.value > state.productHint.stock;
        });

        // Warns when the unit price falls outside the product's allowed selling band
        // (see ValidateSellingPriceAsync on the server, which enforces it on save).
        const posPriceOutOfBand = Vue.computed(() => {
            const price = state.productPick.unitPrice;
            if (price === null || price === undefined || price === '') return false;
            const belowMin = state.productHint.minPrice !== null && price < state.productHint.minPrice;
            const aboveMax = state.productHint.maxPrice !== null && price > state.productHint.maxPrice;
            return belowMin || aboveMax;
        });

        // One mapping for the grid, stat strip and view modal (SalesOrderStatus: Draft/Cancelled/Confirmed/Archived).
        const statusPillClass = (statusName) => {
            const key = String(statusName ?? '').toLowerCase();
            return ['draft', 'confirmed', 'cancelled', 'archived'].includes(key)
                ? `status-pill status-pill--${key}`
                : 'status-pill';
        };

        // Delivery progress pill (Complete / Partial / Not Started / N/A) — see order-list.css.
        const deliveryPillClass = (statusName) => {
            const key = String(statusName ?? '').toLowerCase().replace(/[^a-z]/g, '');
            return ['complete', 'partial', 'notstarted', 'na'].includes(key)
                ? `status-pill status-pill--${key}`
                : 'status-pill';
        };

        const escapeHtml = (value) => String(value ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));

        const listStats = Vue.computed(() => {
            const rows = state.mainData ?? [];
            const isStatus = (row, name) => String(row.orderStatusName ?? '').toLowerCase() === name;
            const confirmed = rows.filter(row => isStatus(row, 'confirmed'));
            return {
                total: rows.length,
                draft: rows.filter(row => isStatus(row, 'draft')).length,
                confirmed: confirmed.length,
                confirmedValue: confirmed.reduce((sum, row) => sum + (row.afterTaxAmount || 0), 0)
            };
        });

        const formatQty = (value) => Number(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });

        // Quantity of a product already sitting in the cart, so stock checks account for it.
        const quantityInCart = (productId, exceptLineId) => state.secondaryData
            .filter(line => line.productId === productId && line.id !== exceptLineId)
            .reduce((sum, line) => sum + (line.quantity || 0), 0);

        const resetProductPick = () => {
            // Invalidate any in-flight price lookup so it can't write into the next pick.
            state.priceResolveSeq++;
            state.productPick = { productId: null, unitPrice: 0, commission: 0, quantity: 1, priceEdited: false };
            state.productHint = { ...emptyProductHint };
            state.priceSuggestion = { loading: false, sourceName: '', costPrice: null, profit: null, profitPercent: null };
            state.productSearch.term = '';
            state.productSearch.open = false;
            state.productSearch.activeIndex = 0;
            state.pickError = '';
        };

        const focusEl = (elRef, select = false) => Vue.nextTick(() => {
            const el = elRef.value;
            if (!el) return;
            el.focus();
            if (select) el.select?.();
        });

        // ── Customer: mobile number field ────────────────────────────────────────────────
        // Mirrors CustomerMobileNumber.TryNormalize on the server, which re-validates on save.
        const MOBILE_PATTERN = /^(?:\+?88)?(01[3-9]\d{8})$/;
        const normalizeMobile = (value) => {
            const match = String(value ?? '').trim().replace(/[\s\-().]/g, '').match(MOBILE_PATTERN);
            return match ? match[1] : null;
        };
        const localDigits = (value) => {
            const digits = String(value ?? '').replace(/\D/g, '');
            return digits.startsWith('880') ? digits.slice(2) : digits;
        };
        const hasLetters = (value) => /[a-z]/i.test(String(value ?? ''));

        let customerSearchTimer = null;
        let customerSearchSeq = 0;

        const emptyCustomer = () => ({
            input: '', status: 'idle', id: null, name: '', number: '', newName: '',
            suggestions: [], open: false, activeIndex: -1
        });

        const resetCustomer = () => {
            clearTimeout(customerSearchTimer);
            customerSearchSeq++;
            state.customer = emptyCustomer();
            state.customerId = null;
        };

        const setExistingCustomer = (c) => {
            clearTimeout(customerSearchTimer);
            customerSearchSeq++;
            state.customer = {
                ...emptyCustomer(),
                input: c.phoneNumber || state.customer.input,
                status: 'existing',
                id: c.id,
                name: c.name ?? '',
                number: c.number ?? ''
            };
            state.customerId = c.id;
            state.errors.customerId = '';
        };

        // Status for typed text once no existing customer is selected.
        const evaluateCustomerInput = () => {
            const text = state.customer.input.trim();
            if (!text) return 'idle';
            if (normalizeMobile(text)) return 'new';
            if (hasLetters(text)) return 'partial';
            return localDigits(text).length >= 11 ? 'invalid' : 'partial';
        };

        const customerErrorMessage = () => {
            const text = state.customer.input.trim();
            if (!text) return 'Enter the customer\'s mobile number.';
            if (hasLetters(text)) return 'Pick a customer from the list, or enter a mobile number.';
            if (localDigits(text).length < 11) return 'Enter the full 11-digit mobile number (e.g. 01711234567).';
            return 'Enter a valid mobile number (e.g. 01711234567 or +8801711234567).';
        };

        // Selected customer → its id; otherwise the typed number, which the server resolves to an
        // existing customer or creates in the same save as the order.
        const customerPayload = () => state.customer.id
            ? { customerId: state.customer.id, customerPhoneNumber: null, customerName: null }
            : { customerId: null, customerPhoneNumber: state.customer.input.trim(), customerName: state.customer.newName.trim() || null };

        const loadCustomer = async (customerId, fallbackName) => {
            resetCustomer();
            if (!customerId) return;
            let c = state.customerListLookupData.find(x => x.id === customerId);
            if (!c) {
                try {
                    const response = await services.getCustomerSingle(customerId);
                    c = response?.data?.content?.data;
                } catch (error) {
                    c = null;
                }
            }
            setExistingCustomer({ id: customerId, name: c?.name ?? fallbackName ?? '', number: c?.number ?? '', phoneNumber: c?.phoneNumber ?? '' });
        };

        const customerField = {
            onInput: () => {
                const c = state.customer;
                c.id = null;
                c.name = '';
                c.number = '';
                state.customerId = null;
                state.errors.customerId = '';
                clearTimeout(customerSearchTimer);

                const text = c.input.trim();
                const searchable = hasLetters(text) ? text.length >= 2 : localDigits(text).length >= 3;
                if (!searchable) {
                    customerSearchSeq++;
                    c.suggestions = [];
                    c.open = false;
                    c.activeIndex = -1;
                    c.status = evaluateCustomerInput();
                    return;
                }

                c.status = 'searching';
                const seq = ++customerSearchSeq;
                customerSearchTimer = setTimeout(() => customerField.search(text, seq), 250);
            },
            search: async (text, seq) => {
                const c = state.customer;
                try {
                    const response = await services.searchCustomer(text);
                    if (seq !== customerSearchSeq) return;
                    const rows = response?.data?.content?.data ?? [];
                    // A complete number that already belongs to a customer selects it outright.
                    const exact = rows.find(r => r.isExactMobileMatch);
                    if (exact) {
                        setExistingCustomer(exact);
                        return;
                    }
                    c.suggestions = rows;
                    c.activeIndex = rows.length ? 0 : -1;
                    c.open = rows.length > 0 && document.activeElement === customerInputRef.value;
                    c.status = evaluateCustomerInput();
                } catch (error) {
                    if (seq !== customerSearchSeq) return;
                    c.suggestions = [];
                    c.open = false;
                    c.status = 'error';
                }
            },
            select: (row) => {
                setExistingCustomer(row);
                focusEl(productSearchRef);
            },
            // Enter with no highlighted suggestion: settle any pending search, then move on to
            // products when the customer is usable, or explain what is missing.
            commit: async () => {
                if (state.customer.status === 'searching') {
                    clearTimeout(customerSearchTimer);
                    await customerField.search(state.customer.input.trim(), ++customerSearchSeq);
                }
                const c = state.customer;
                c.open = false;
                if (c.id || normalizeMobile(c.input)) {
                    focusEl(productSearchRef);
                    return;
                }
                state.errors.customerId = customerErrorMessage();
            },
            onKeydown: (e) => {
                const c = state.customer;
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
                        customerField.select(c.suggestions[c.activeIndex]);
                    } else {
                        customerField.commit();
                    }
                }
            },
            onFocus: () => {
                const c = state.customer;
                if (!c.id && c.suggestions.length) c.open = true;
            },
            // Suggestion clicks use mousedown.prevent, so blur here is a real focus change.
            onBlur: () => {
                state.customer.open = false;
            },
            clear: () => {
                resetCustomer();
                focusEl(customerInputRef);
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

        // Empty product search + Enter: the cart is done, go to payment (or Save when there is none to take).
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
            state.errors.customerId = '';
            state.errors.taxId = '';
            state.errors.orderStatus = '';

            let isValid = true;

            if (!state.orderDate) {
                state.errors.orderDate = 'Order date is required.';
                isValid = false;
            }
            if (!state.customer.id && !normalizeMobile(state.customer.input)) {
                state.errors.customerId = customerErrorMessage();
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
            if (state.errors.customerId) focusEl(customerInputRef, true);
            else if (state.errors.orderDate) orderDatePicker.focus();
            else if (state.errors.orderStatus) salesOrderStatusListLookup.obj?.focusIn();
            else if (state.errors.taxId) taxListLookup.obj?.focusIn();
        };

        // Status and tax rarely change between orders: start each new order with the last ones used.
        const DEFAULTS_KEY = 'salesOrder.defaults';
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
            const statuses = state.salesOrderStatusListLookupData ?? [];
            const taxes = state.taxListLookupData ?? [];
            const savedStatus = statuses.find(x => String(x.id) === String(saved.orderStatus));
            const draft = statuses.find(x => String(x.name).toLowerCase() === 'draft');
            state.orderStatus = savedStatus ? String(savedStatus.id) : (draft ? String(draft.id) : null);
            // Tax: last used on this device → tax of the most recent sales order → the only tax.
            const savedTax = taxes.find(x => x.id === saved.taxId);
            const latestOrder = [...(state.mainData ?? [])].sort((a, b) => (b.createdAtUtc ?? 0) - (a.createdAtUtc ?? 0))[0];
            const recentTax = latestOrder && taxes.find(x => x.id === latestOrder.taxId);
            state.taxId = savedTax?.id ?? recentTax?.id ?? (taxes.length === 1 ? taxes[0].id : null);
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
            state.previousDue = 0;
            resetCustomer();
            state.taxId = null;
            state.orderStatus = null;
            state.errors = {
                orderDate: '',
                customerId: '',
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

        let pendingPrice = null;

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesOrderSingle: async (id) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    const response = await AxiosManager.get(`/SalesOrder/GetSalesOrderSingle?id=${encodeURIComponent(id)}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            // customer: { customerId, customerPhoneNumber, customerName } from customerPayload().
            createMainData: async (orderDate, description, orderStatus, taxId, customer, createdById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/CreateSalesOrder', {
                        orderDate: DateFormatManager.toApiDate(orderDate), description, orderStatus, taxId, ...customer, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, orderDate, description, orderStatus, taxId, customer, updatedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/UpdateSalesOrder', {
                        id, orderDate: DateFormatManager.toApiDate(orderDate), description, orderStatus, taxId, ...customer, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/DeleteSalesOrder', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deliverNowMainData: async (salesOrderId, deliveryDate, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrder/DeliverNow', {
                        salesOrderId, deliveryDate: DateFormatManager.toApiDate(deliveryDate), description, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Customer/GetCustomerList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            searchCustomer: async (term) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    const response = await AxiosManager.get(`/Customer/SearchCustomer?term=${encodeURIComponent(term)}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerSingle: async (id) => {
                try {
                    const response = await AxiosManager.get(`/Customer/GetCustomerSingle?id=${encodeURIComponent(id)}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerDue: async (customerId, excludeSalesOrderId) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    let url = `/Customer/GetCustomerDue?customerId=${encodeURIComponent(customerId)}`;
                    if (excludeSalesOrderId) {
                        url += `&excludeSalesOrderId=${encodeURIComponent(excludeSalesOrderId)}`;
                    }
                    const response = await AxiosManager.get(url, {});
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
            getSalesOrderStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSecondaryData: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get('/SalesOrderItem/GetSalesOrderItemBySalesOrderIdList?salesOrderId=' + salesOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createSecondaryData: async (unitPrice, commissionRate, quantity, remark, productId, salesOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/CreateSalesOrderItem', {
                        unitPrice, commissionRate, quantity, remark, productId, salesOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, unitPrice, commissionRate, quantity, remark, productId, salesOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/UpdateSalesOrderItem', {
                        id, unitPrice, commissionRate, quantity, remark, productId, salesOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/SalesOrderItem/DeleteSalesOrderItem', {
                        id, deletedById
                    });
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
            getPaymentListByModule: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentListByModule?moduleName=SalesOrder&moduleId=${salesOrderId}`, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentSummaryData: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get(
                        `/Payment/GetPaymentSummary?moduleName=SalesOrder&moduleId=${salesOrderId}`, {});
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
            getProductListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Product/GetProductList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getInventoryStockList: async () => {
                try {
                    const response = await AxiosManager.get('/InventoryTransaction/GetInventoryStockList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductAvailability: async (productId, excludeSalesOrderId) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    let url = `/SalesOrder/GetProductAvailability?productId=${encodeURIComponent(productId)}`;
                    if (excludeSalesOrderId) {
                        url += `&excludeSalesOrderId=${encodeURIComponent(excludeSalesOrderId)}`;
                    }
                    const response = await AxiosManager.get(url, {});
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
            resolveSuggestedPrice: async (productId, customerId, quantity, saleDate) => {
                try {
                    // AxiosManager.get ignores config.params, so the query is built into the URL.
                    let url = `/Price/ResolvePrice?productId=${encodeURIComponent(productId)}&quantity=${encodeURIComponent(quantity ?? 1)}`;
                    if (customerId) {
                        url += `&customerId=${encodeURIComponent(customerId)}`;
                    }
                    const apiDate = DateFormatManager.toApiDate(saleDate);
                    if (apiDate) {
                        url += `&saleDate=${encodeURIComponent(apiDate)}`;
                    }
                    const response = await AxiosManager.get(url, {});
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
            }
        };

        const methods = {
            populateCustomerListLookupData: async () => {
                const response = await services.getCustomerListLookupData();
                state.customerListLookupData = response?.data?.content?.data ?? [];
            },
            populatePreviousDue: async (customerId) => {
                // No customer selected → nothing owed to show.
                if (!customerId) {
                    state.previousDue = 0;
                    return;
                }
                try {
                    // Exclude the order currently open for edit so it isn't counted as its own "previous" due.
                    const response = await services.getCustomerDue(customerId, state.id || null);
                    state.previousDue = response?.data?.content?.data?.previousDue ?? 0;
                } catch (error) {
                    state.previousDue = 0;
                }
            },
            populateTaxListLookupData: async () => {
                const response = await services.getTaxListLookupData();
                state.taxListLookupData = response?.data?.content?.data;
            },
            populateSalesOrderStatusListLookupData: async () => {
                const response = await services.getSalesOrderStatusListLookupData();
                state.salesOrderStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSecondaryData: async (salesOrderId) => {
                try {
                    const response = await services.getSecondaryData(salesOrderId);
                    state.secondaryData = response?.data?.content?.data.map(item => ({
                        ...item,
                        createdAtUtc: new Date(item.createdAtUtc)
                    }));
                    methods.refreshPaymentSummary(salesOrderId);
                } catch (error) {
                    state.secondaryData = [];
                }
            },
            populateProductListLookupData: async () => {
                const [productResponse, stockResponse] = await Promise.all([
                    services.getProductListLookupData(),
                    services.getInventoryStockList()
                ]);

                const products = productResponse?.data?.content?.data ?? [];
                const stockList = stockResponse?.data?.content?.data ?? [];

                const stockByProduct = {};
                stockList.forEach(s => {
                    const pid = s.productId;
                    stockByProduct[pid] = (stockByProduct[pid] ?? 0) + (s.stock ?? 0);
                });

                state.productListLookupData = products.map(p => ({
                    ...p,
                    availableStock: stockByProduct[p.id] ?? 0
                }));
            },
            refreshSuggestedPrice: (autoApply = true) => {
                if (!state.productPick.productId) {
                    return Promise.resolve();
                }
                // Kept so "Add" can wait for the price that is about to be applied.
                pendingPrice = methods.resolveSuggestedPrice(autoApply);
                return pendingPrice;
            },
            resolveSuggestedPrice: async (autoApply) => {
                const productId = state.productPick.productId;
                const seq = ++state.priceResolveSeq;
                state.priceSuggestion.loading = true;
                try {
                    const response = await services.resolveSuggestedPrice(
                        productId,
                        state.customerId,
                        state.productPick.quantity || 1,
                        state.orderDate);
                    if (seq !== state.priceResolveSeq) {
                        return;
                    }
                    const content = response?.data?.content ?? {};
                    state.priceSuggestion = {
                        loading: false,
                        sourceName: content.priceSourceName ?? '',
                        costPrice: content.costPrice ?? null,
                        profit: content.profit ?? null,
                        profitPercent: content.profitPercent ?? null
                    };
                    // Auto-apply the resolved policy/promotion/QB price unless the cashier has already
                    // typed one (Min/Max band is still enforced on the server).
                    if (autoApply && !state.productPick.priceEdited) {
                        const resolvedPrice = content.calculatedPrice !== null && content.calculatedPrice !== undefined
                            ? Number(content.calculatedPrice)
                            : 0;
                        if (resolvedPrice > 0) {
                            state.productPick.unitPrice = resolvedPrice;
                        } else {
                            // No promotion/policy/quantity-break price for this context: fall back to the
                            // product's minimum allowed selling price when set, else its listed price.
                            const product = state.productListLookupData.find(p => p.id === productId);
                            state.productPick.unitPrice = product?.minSellingPrice ?? product?.unitPrice ?? 0;
                        }
                    }
                } catch (error) {
                    if (seq !== state.priceResolveSeq) {
                        return;
                    }
                    state.priceSuggestion = { loading: false, sourceName: '', costPrice: null, profit: null, profitPercent: null };
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
            populatePaymentMethodListLookupData: async () => {
                const response = await services.getPaymentMethodListLookupData();
                state.paymentMethodListLookupData = response?.data?.content?.data ?? [];
                if (!state.newPayment.paymentMethodId) state.newPayment.paymentMethodId = defaultPaymentMethodId();
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
                            notify('success', 'Sales order deleted');
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
                            notify('success', `Sales order ${state.number} created`);
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
                        state.id, state.orderDate, state.description, state.orderStatus, state.taxId, customerPayload(), StorageManager.getUserId());

                    if (response.data.code === 200) {
                        const data = response.data.content.data;
                        applySavedHeader(data);
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
            // Save (or update) the order, then ship everything still outstanding for it in one
            // action. The sale is saved first; if delivery then fails the order stays saved and
            // the user can retry without creating a second order.
            deliverNow: async () => {
                if (state.isDelivering || state.isSubmitting) return;
                if (state.deleteMode) return;

                if (!validateForm()) {
                    focusFirstError();
                    return;
                }

                state.isDelivering = true;
                let savedNumber = '';

                try {
                    // 1. Persist the sale (reusing the normal save paths).
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
                            state.id, state.orderDate, state.description, state.orderStatus, state.taxId, customerPayload(), StorageManager.getUserId());

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

                    // 2. Deliver the remaining quantities through the existing delivery workflow.
                    let deliverResponse;
                    try {
                        deliverResponse = await services.deliverNowMainData(
                            state.id, state.orderDate, state.description, StorageManager.getUserId());
                    } catch (error) {
                        // The sale is saved; only delivery failed. Keep it and offer a retry.
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'warning',
                            title: 'Sale Saved, Delivery Failed',
                            html: `Sales order <strong>${savedNumber}</strong> was saved, but delivery is still pending.<br>`
                                + `${(error.response?.data?.message ?? 'Please try again.')}`,
                            confirmButtonText: 'OK'
                        });
                        return;
                    }

                    const result = deliverResponse?.data?.content;
                    await methods.populateMainData();
                    mainGrid.refresh();
                    await methods.refreshPaymentSummary(state.id);

                    if (deliverResponse.data.code === 200 && result?.delivered) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Delivered',
                            html: `Sales order <strong>${savedNumber}</strong> delivered.<br>Delivery order: <strong>${result.deliveryOrderNumber ?? ''}</strong>`
                        });
                    } else if (result?.alreadyDelivered) {
                        Swal.fire({
                            icon: 'info',
                            title: 'Already Delivered',
                            html: `Sales order <strong>${savedNumber}</strong> has no outstanding quantities to deliver.`
                        });
                    } else {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Sale Saved, Delivery Incomplete',
                            text: deliverResponse.data.message ?? 'The order was saved but delivery did not complete.'
                        });
                    }
                } finally {
                    state.isDelivering = false;
                }
            },
            onMainModalHidden: () => {
                clearTimeout(customerSearchTimer);
                customerSearchSeq++;
                state.customer.open = false;
                state.productSearch.open = false;
                state.errors.orderDate = '';
                state.errors.customerId = '';
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

        const salesOrderStatusListLookup = {
            obj: null,
            create: () => {
                if (state.salesOrderStatusListLookupData && Array.isArray(state.salesOrderStatusListLookupData)) {
                    salesOrderStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        allowFiltering: true,
                        filterType: 'Contains',
                        dataSource: state.salesOrderStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select an Order Status',
                        value: state.orderStatus,
                        change: (e) => {
                            state.orderStatus = e.value;
                        }
                    });
                    salesOrderStatusListLookup.obj.appendTo(orderStatusRef.value);
                }
            },
            refresh: () => {
                if (salesOrderStatusListLookup.obj) {
                    salesOrderStatusListLookup.obj.value = state.orderStatus;
                }
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

        let qtyResolveTimer = null;

        watcherStops.push(Vue.watch(
            () => state.productPick.quantity,
            () => {
                // Quantity breaks depend on qty — debounce so typing 10/100 doesn't spam the API.
                if (!state.productPick.productId) {
                    return;
                }
                if (qtyResolveTimer) {
                    clearTimeout(qtyResolveTimer);
                }
                qtyResolveTimer = setTimeout(() => {
                    methods.refreshSuggestedPrice();
                }, 500);
            }
        ));

        watcherStops.push(Vue.watch(
            () => state.orderDate,
            (newVal, oldVal) => {
                orderDatePicker.refresh();
                state.errors.orderDate = '';
            }
        ));

        watcherStops.push(Vue.watch(
            () => state.customerId,
            (newVal, oldVal) => {
                // Show the customer's outstanding receivable (0 when none / when cleared).
                methods.populatePreviousDue(newVal);
                // Re-price the picked product for the new customer (group policy may differ).
                if (state.productPick.productId) {
                    methods.refreshSuggestedPrice();
                }
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
                salesOrderStatusListLookup.refresh();
                state.errors.orderStatus = '';
            }
        ));

        // Load a list record into the order modal for editing. Shared by the Edit toolbar
        // button, the View modal's "Edit Order" button, and double-click / SO-number click
        // on a Draft row.
        const openEditForRecord = async (record) => {
            if (!record) return;
            state.deleteMode = false;
            state.paymentError = '';
            resetNewPaymentState();
            resetProductPick();
            state.mainTitle = 'Edit Sales Order';
            state.id = record.id ?? '';
            state.number = record.number ?? '';
            state.orderDate = record.orderDate ? new Date(record.orderDate) : null;
            state.description = record.description ?? '';
            state.taxId = record.taxId ?? '';
            taxListLookup.trackingChange = true;
            state.orderStatus = String(record.orderStatus ?? '');

            await Promise.all([
                loadCustomer(record.customerId, record.customerName),
                methods.populateSecondaryData(record.id)
            ]);

            mainModal.obj.show();
        };

        // Double-click a row / click the SO number: Draft orders are still being worked on, so
        // they open straight into Edit; Confirmed/Cancelled/Archived open the read-only View
        // (which carries its own "Edit Order" button).
        const openForRecord = (record) => {
            if (!record?.id) return;
            if (String(record.orderStatusName ?? '').toLowerCase() === 'draft') {
                openEditForRecord(record);
            } else {
                handlerRef.openViewModal(record.id);
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
                        { field: 'orderDate', headerText: 'SO Date', width: 150, format: 'dd/MM/yyyy' },
                        { field: 'customerName', headerText: 'Customer', width: 200, minWidth: 200 },
                        { field: 'orderStatusName', headerText: 'Status', width: 140, minWidth: 120 },
                        { field: 'totalOrderedQty', headerText: 'Ordered Qty', width: 110, minWidth: 100, format: 'N2', textAlign: 'Right', type: 'number' },
                        { field: 'totalDeliveredQty', headerText: 'Delivered Qty', width: 120, minWidth: 110, format: 'N2', textAlign: 'Right', type: 'number' },
                        { field: 'deliveryStatus', headerText: 'Delivery', width: 130, minWidth: 120 },
                        { field: 'taxName', headerText: 'Tax', width: 150, minWidth: 150 },
                        { field: 'afterTaxAmount', headerText: 'Total Amount', width: 150, minWidth: 150, format: 'N2', textAlign: 'Right' },
                        { field: 'createdAtUtc', headerText: 'Created At', width: 150, format: 'dd/MM/yyyy HH:mm' },
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
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                    },
                    queryCellInfo: (args) => {
                        if (args.column.field === 'orderStatusName') {
                            const name = args.data?.orderStatusName;
                            args.cell.innerHTML = `<span class="${statusPillClass(name)}">${escapeHtml(name || '—')}</span>`;
                        }
                        if (args.column.field === 'deliveryStatus') {
                            const name = args.data?.deliveryStatus;
                            args.cell.innerHTML = `<span class="${deliveryPillClass(name)}">${escapeHtml(name || '—')}</span>`;
                        }
                        if (args.column.field === 'number') {
                            args.cell.style.cursor = 'pointer';
                            args.cell.style.color = 'var(--primary)';
                            args.cell.style.fontWeight = '600';
                            args.cell.onclick = () => {
                                openForRecord(args.rowData);
                            };
                        }
                    },
                    beforeExcelExport: (args) => {
                        args.data = args.data.map(function (row) {
                            return Object.assign({}, row, {
                                orderDate: row.orderDate instanceof Date
                                    ? row.orderDate.toLocaleDateString('en-CA')
                                    : row.orderDate,
                                createdAtUtc: row.createdAtUtc instanceof Date
                                    ? row.createdAtUtc.toLocaleDateString('en-CA') + ' ' + row.createdAtUtc.toTimeString().slice(0, 5)
                                    : row.createdAtUtc
                            });
                        });
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
                            mainGrid.obj.excelExport({ fileName: `SalesOrders_${date}.xlsx` });
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
                                state.mainTitle = 'Delete Sales Order?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.orderDate = selectedRecord.orderDate ? new Date(selectedRecord.orderDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.taxId = selectedRecord.taxId ?? '';
                                state.orderStatus = String(selectedRecord.orderStatus ?? '');

                                await Promise.all([
                                    loadCustomer(selectedRecord.customerId, selectedRecord.customerName),
                                    methods.populateSecondaryData(selectedRecord.id)
                                ]);

                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/SalesOrders/SalesOrderPdf?id=' + (selectedRecord.id ?? ''), '_blank');
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

        const refreshCustomerLookups = async () => {
            try {
                await methods.populateCustomerListLookupData();
                filterCustomerDropdown.obj?.setProperties({ dataSource: state.customerListLookupData });
            } catch (error) {
                // The filter list is secondary; it refreshes on the next page load.
            }
        };

        // Copy a saved header back into the form. A customer that was typed as a new number now
        // exists on the server, so the field switches to "existing".
        const applySavedHeader = (data) => {
            const typedNewCustomer = !state.customer.id;
            state.id = data.id ?? '';
            state.number = data.number ?? '';
            state.orderDate = data.orderDate ? new Date(data.orderDate) : null;
            state.description = data.description ?? '';
            state.taxId = data.taxId ?? '';
            taxListLookup.trackingChange = true;
            state.orderStatus = String(data.orderStatus ?? '');
            state.mainTitle = `Sales Order ${state.number}`;

            if (typedNewCustomer && data.customerId) {
                const mobile = normalizeMobile(state.customer.input);
                setExistingCustomer({
                    id: data.customerId,
                    name: data.customer?.name ?? (state.customer.newName.trim() || mobile),
                    number: data.customer?.number ?? '',
                    phoneNumber: data.customer?.phoneNumber ?? mobile
                });
                refreshCustomerLookups();
            }
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
                        state.orderDate, state.description, state.orderStatus, state.taxId, customerPayload(), StorageManager.getUserId());

                    if (response.data.code === 200) {
                        applySavedHeader(response.data.content.data);
                        rememberOrderDefaults();
                        await methods.populateMainData();
                        mainGrid.refresh();
                        await methods.refreshPaymentLedger(state.id);
                        return { ok: true };
                    }
                    return { ok: false, message: response.data.message ?? 'Could not create the sales order.' };
                } catch (error) {
                    const message = error.response?.data?.message ?? 'Could not create the sales order. Please try again.';
                    // A rejected mobile number belongs next to the customer field.
                    if (/mobile|customer/i.test(message)) {
                        state.errors.customerId = message;
                        focusEl(customerInputRef, true);
                    }
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

        // Client-side stock nudge; the server's available-to-promise check is the authority.
        const stockShortfall = (product, quantity, exceptLineId) => {
            if (!product.physical) return '';
            const inCart = quantityInCart(product.id, exceptLineId);
            const hint = state.productHint;
            // Prefer available-to-promise (stock less pending deliveries) when it is loaded for this product.
            if (hint.productId === product.id && hint.available !== null) {
                const available = hint.available - inCart;
                if (quantity <= available) return '';
                const pending = (hint.pendingDraft || 0) + (hint.pendingConfirmed || 0);
                return `Only ${formatQty(Math.max(0, available))} of ${product.name} available ` +
                    `(${formatQty(hint.stock)} in stock, ${formatQty(pending)} pending delivery` +
                    (inCart ? `, ${formatQty(inCart)} already in the cart).` : ').');
            }
            const onHand = product.availableStock ?? 0;
            if (quantity + inCart <= onHand) return '';
            return `Only ${formatQty(onHand)} of ${product.name} in stock` +
                (inCart ? ` (${formatQty(inCart)} already in the cart).` : '.');
        };

        // On hand / pending delivery / available for the picked product, from the same calculation
        // the server uses when the line is saved. Lines of this order are excluded (they show as "in cart").
        let availabilitySeq = 0;
        const loadAvailability = async (productId) => {
            const seq = ++availabilitySeq;
            state.productHint.availabilityLoading = true;
            try {
                const response = await services.getProductAvailability(productId, state.id || null);
                if (seq !== availabilitySeq || state.productHint.productId !== productId) return;
                const data = response?.data?.content?.data;
                if (data && data.physical) {
                    state.productHint.stock = data.onHand ?? 0;
                    state.productHint.pendingDraft = data.committedDraft ?? 0;
                    state.productHint.pendingConfirmed = data.committedConfirmed ?? 0;
                    state.productHint.available = data.availableToPromise ?? 0;
                }
            } catch (error) {
                // Keep the list's stock figure; the server still checks on save.
            } finally {
                if (seq === availabilitySeq) state.productHint.availabilityLoading = false;
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
                state.priceResolveSeq++;
                state.productPick = {
                    productId: product.id,
                    unitPrice: product.minSellingPrice ?? product.unitPrice ?? 0,
                    commission: 0,
                    quantity: 1,
                    priceEdited: false
                };
                state.productHint = {
                    ...emptyProductHint,
                    productId: product.id,
                    name: product.name,
                    physical: product.physical ?? false,
                    stock: product.physical ? (product.availableStock ?? 0) : null,
                    minPrice: product.minSellingPrice ?? null,
                    maxPrice: product.maxSellingPrice ?? null
                };
                if (product.physical) loadAvailability(product.id);
                state.productSearch.term = product.name ?? '';
                state.productSearch.open = false;
                state.pickError = '';
                focusEl(pickQtyRef, true);
                // Override the fallback with the resolved policy/promotion/QB price.
                methods.refreshSuggestedPrice();
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
                const shortfall = stockShortfall(product, quantity);
                if (shortfall) {
                    state.pickError = shortfall;
                    focusEl(focusOnError, true);
                    return false;
                }

                state.isAddingLine = true;
                try {
                    const header = await ensureHeaderSaved();
                    if (!header.ok) {
                        // Say why nothing was added; focus already moved to the field to fix.
                        state.pickError = header.message
                            ?? (state.errors.customerId ? 'Enter the customer\'s mobile number first.' : missingDetailsMessage());
                        return false;
                    }

                    await services.createSecondaryData(unitPrice, commissionRate, quantity, null, product.id, state.id, StorageManager.getUserId());
                    await refreshAfterCartChange();

                    resetProductPick();
                    flashCart(`Added ${product.name}`);
                    focusEl(productSearchRef);
                    return true;
                } catch (error) {
                    // Keep the pick so the cashier can adjust and retry.
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

                // Let a price lookup that is still running land first, so the line gets that price.
                if (pendingPrice) await pendingPrice;

                const quantity = Number(state.productPick.quantity);
                const unitPrice = Number(state.productPick.unitPrice);
                const commissionRate = Number(state.productPick.commission) || 0;

                if (!quantity || quantity <= 0) {
                    state.pickError = 'Enter a quantity greater than zero.';
                    focusEl(pickQtyRef, true);
                    return;
                }
                if (isNaN(unitPrice) || unitPrice < 0) {
                    state.pickError = 'Enter a valid unit price.';
                    focusEl(pickPriceRef, true);
                    return;
                }
                if (commissionRate < 0 || commissionRate > 100) {
                    state.pickError = 'Commission % must be between 0 and 100.';
                    return;
                }

                await cart.createLine(product, unitPrice, commissionRate, quantity, pickQtyRef);
            },
            // Scan / exact-SKU path: one unit at the resolved price, no further input needed.
            quickAdd: async (product) => {
                if (state.isAddingLine) return;
                state.pickError = '';
                let unitPrice = product.minSellingPrice ?? product.unitPrice ?? 0;
                try {
                    const priceResponse = await services.resolveSuggestedPrice(product.id, state.customerId, 1, state.orderDate);
                    const resolved = priceResponse?.data?.content?.calculatedPrice;
                    if (resolved !== null && resolved !== undefined && Number(resolved) > 0) {
                        unitPrice = Number(resolved);
                    }
                } catch (resolveError) {
                    // Keep the product fallback; pricing is advisory, never blocking.
                }
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

                const product = state.productListLookupData.find(p => p.id === line.productId);
                const shortfall = product ? stockShortfall(product, quantity, line.id) : '';
                if (shortfall) {
                    Swal.fire({ icon: 'error', title: 'Insufficient Stock', text: shortfall });
                    await methods.populateSecondaryData(state.id);
                    return;
                }

                await persistLine(line, { quantity });
            },
            commitLinePrice: async (line, value) => {
                const unitPrice = Number(value);
                if (isNaN(unitPrice) || unitPrice < 0) {
                    Swal.fire({ icon: 'warning', title: 'Enter a valid unit price' });
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

        const onMainModalShown = () => {
            if (state.deleteMode) return;
            if (!state.customer.id && !state.customer.input) focusEl(customerInputRef);
            else focusEl(productSearchRef);
        };

        // The returned handler object, so handlers can call each other.
        let handlerRef = null;

        const openAddModal = () => {
            state.deleteMode = false;
            state.mainTitle = 'New Sales Order';
            resetFormState();
            applyOrderDefaults();
            mainModal.obj.show();
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
                await SecurityManager.authorizePage(['SalesOrders']);
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

                // Quick Shortcut "open add form" intent: /SalesOrders/SalesOrderList?quickAction=add
                // lands here and opens the New Sales Order modal straight away. Order defaults are
                // re-applied once the lookups below resolve (the same path a toolbar Add uses).
                if (new URLSearchParams(window.location.search).get('quickAction') === 'add') {
                    openAddModal();
                }

                Promise.all([
                    methods.populateCustomerListLookupData(),
                    methods.populateTaxListLookupData(),
                    methods.populateSalesOrderStatusListLookupData(),
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
                    taxListLookup.create();
                    salesOrderStatusListLookup.create();
                    taxQuickModal.create();
                    createFilterControls();
                });
            } catch (e) {
            } finally {

            }
        });

        Vue.onUnmounted(() => {
            if (qtyResolveTimer) {
                clearTimeout(qtyResolveTimer);
            }
            clearTimeout(customerSearchTimer);
            clearTimeout(cartFlashTimer);
            watcherStops.forEach(stop => stop());
            window.removeEventListener('resize', onWindowResize);
            mainModalRef.value?.removeEventListener('hidden.bs.modal', methods.onMainModalHidden);
            mainModalRef.value?.removeEventListener('shown.bs.modal', onMainModalShown);
            mainGrid.obj?.destroy();
            orderDatePicker.obj?.destroy();
            taxListLookup.obj?.destroy();
            salesOrderStatusListLookup.obj?.destroy();
            filterStatusDropdown.obj?.destroy();
            filterCustomerDropdown.obj?.destroy();
            filterTaxDropdown.obj?.destroy();
            filterFromDatePicker.obj?.destroy();
            filterToDatePicker.obj?.destroy();
            if (paymentDateRef.value) {
                const dpInst = paymentDateRef.value?.ej2_instances?.[0];
                dpInst?.destroy();
            }
        });

        return {
            mainGridRef,
            filterPanelRef,
            filterStatusRef,
            filterCustomerRef,
            filterTaxRef,
            filterFromDateRef,
            filterToDateRef,
            activeFilterCount,
            paymentDateRef,
            mainModalRef,
            orderDateRef,
            taxIdRef,
            orderStatusRef,
            customerInputRef,
            productSearchRef,
            pickQtyRef,
            pickPriceRef,
            paymentAmountRef,
            saveButtonRef,
            productMatches,
            posLineTotal,
            posStockExceeded,
            posPriceOutOfBand,
            posInCart,
            posAvailable,
            taxQuickModalRef,
            viewModalRef,
            state,
            methods,
            listStats,
            handler: handlerRef = {
                handleSubmit: methods.handleFormSubmit,
                deliverNow: methods.deliverNow,
                toggleFilter: filterHandler.toggle,
                clearFilters: filterHandler.clear,
                openAddModal: openAddModal,
                onModalKeydown: onModalKeydown,
                customerInput: customerField.onInput,
                customerKeydown: customerField.onKeydown,
                customerFocus: customerField.onFocus,
                customerBlur: customerField.onBlur,
                selectCustomer: customerField.select,
                clearCustomer: customerField.clear,
                productInput: productField.onInput,
                productKeydown: productField.onKeydown,
                productBlur: productField.onBlur,
                pickProduct: productField.pick,
                markPriceEdited: () => { state.productPick.priceEdited = true; },
                formatAmount: (value) => NumberFormatManager.formatToLocale(value ?? 0),
                formatQty: formatQty,
                formatDate: (value) => {
                    if (!value) return '—';
                    const d = new Date(value);
                    return isNaN(d) ? value : d.toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });
                },
                editFromView: () => {
                    const id = state.view.id;
                    viewModal.obj.hide();
                    if (id) {
                        const record = state.mainData.find(r => r.id === id);
                        if (record) openEditForRecord(record);
                    }
                },
                stepQty: (delta) => {
                    const next = Math.max(0, (Number(state.productPick.quantity) || 0) + delta);
                    state.productPick.quantity = Number(next.toFixed(4));
                },
                refreshSuggestedPrice: async () => {
                    await methods.refreshSuggestedPrice();
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
                            moduleName: 'SalesOrder',
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
                openTaxQuickCreate: () => {
                    state.taxQuickName = '';
                    state.taxQuickPercentage = null;
                    state.taxQuickDescription = '';
                    state.taxQuickErrors = { name: '', percentage: '' };
                    taxQuickModal.obj.show();
                },
                closeTaxQuickCreate: () => {
                    taxQuickModal.obj.hide();
                },
                submitTaxQuickCreate: async () => {
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
                            taxListLookup.obj.setProperties({ dataSource: state.taxListLookupData, value: newTax.id });
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
                            Swal.fire({ icon: 'error', title: 'Create Failed', text: response.data.message ?? 'Please check your data.', confirmButtonText: 'Try Again' });
                        }
                    } catch (error) {
                        Swal.fire({ icon: 'error', title: 'An Error Occurred', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                    } finally {
                        state.taxQuickIsSubmitting = false;
                    }
                },
                openViewModal: async (id) => {
                    try {
                        const response = await services.getSalesOrderSingle(id);
                        const result = response?.data?.content;
                        const data = result?.data;
                        if (!data) return;

                        const orderStatusName = result?.orderStatusName ?? '';

                        state.view = {
                            id: data.id ?? '',
                            number: data.number ?? '',
                            orderDate: data.orderDate ?? '',
                            orderStatusName: orderStatusName,
                            statusClass: statusPillClass(orderStatusName),
                            description: data.description ?? '',
                            customerName: data.customer?.name ?? '',
                            taxName: data.tax?.name ?? '',
                            subTotalAmount: data.subTotalAmount ?? 0,
                            discountAmount: data.discountAmount ?? 0,
                            beforeTaxAmount: data.beforeTaxAmount ?? 0,
                            taxAmount: data.taxAmount ?? 0,
                            afterTaxAmount: data.afterTaxAmount ?? 0,
                            items: (data.salesOrderItemList ?? []).map(item => ({
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
                        Swal.fire({ icon: 'error', title: 'Error', text: 'Failed to load sales order details.' });
                    }
                }
            }
        };
    }
};

Vue.createApp(App).mount('#app');