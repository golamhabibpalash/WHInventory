const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            productGroupListLookupData: [],
            productListLookupData: [],
            productGroupId: null,
            productId: null,
            isLoading: false,
        });

        const mainGridRef = Vue.ref(null);
        const productGroupIdRef = Vue.ref(null);
        const productIdRef = Vue.ref(null);

        const services = {
            getMainData: async (productGroupId, productId) => {
                const params = new URLSearchParams();
                if (productGroupId) params.append('productGroupId', productGroupId);
                if (productId) params.append('productId', productId);
                return AxiosManager.get('/Price/GetPriceReport?' + params.toString(), {});
            },
            getProductGroupListLookupData: async () => AxiosManager.get('/ProductGroup/GetProductGroupList', {}),
            getProductListLookupData: async () => AxiosManager.get('/Product/GetProductList', {}),
        };

        const methods = {
            populateProductGroupListLookupData: async () => {
                const response = await services.getProductGroupListLookupData();
                state.productGroupListLookupData = response?.data?.content?.data ?? [];
                if (productGroupIdLookup.obj) productGroupIdLookup.obj.setProperties({ dataSource: state.productGroupListLookupData });
            },
            populateProductListLookupData: async () => {
                const response = await services.getProductListLookupData();
                state.productListLookupData = response?.data?.content?.data ?? [];
                if (productIdLookup.obj) productIdLookup.obj.setProperties({ dataSource: state.productListLookupData });
            },
        };

        const productGroupIdLookup = {
            obj: null,
            create: () => {
                productGroupIdLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productGroupListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Groups',
                    showClearButton: true,
                    change: (e) => { state.productGroupId = e.value ?? null; }
                });
                productGroupIdLookup.obj.appendTo(productGroupIdRef.value);
            },
        };

        const productIdLookup = {
            obj: null,
            create: () => {
                productIdLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productListLookupData,
                    fields: { value: 'id', text: 'name' },
                    placeholder: 'All Products',
                    showClearButton: true,
                    change: (e) => { state.productId = e.value ?? null; }
                });
                productIdLookup.obj.appendTo(productIdRef.value);
            },
        };

        const handler = {
            search: async () => {
                try {
                    state.isLoading = true;
                    const response = await services.getMainData(state.productGroupId, state.productId);
                    state.mainData = response?.data?.content?.data ?? [];
                    mainGrid.obj.setProperties({ dataSource: state.mainData });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Please try again.', confirmButtonText: 'OK' });
                } finally {
                    state.isLoading = false;
                }
            },
            reset: () => {
                state.productGroupId = null;
                state.productId = null;
                if (productGroupIdLookup.obj) productGroupIdLookup.obj.value = null;
                if (productIdLookup.obj) productIdLookup.obj.value = null;
                state.mainData = [];
                mainGrid.obj.setProperties({ dataSource: [] });
            },
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '400px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: false,
                    allowGrouping: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    searchSettings: { keyDelay: 150, searchAsType: true },
                    sortSettings: { columns: [{ field: 'productName', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'productId', isPrimaryKey: true, visible: false },
                        { field: 'productNumber', headerText: 'No.', width: 100 },
                        { field: 'productName', headerText: 'Product', width: 220 },
                        { field: 'defaultPrice', headerText: 'Default Price', width: 130, format: 'N4' },
                        { field: 'weightedAverageCost', headerText: 'WAC', width: 120, format: 'N4' },
                        { field: 'activePricePolicyName', headerText: 'Active Policy', width: 150 },
                        { field: 'activeFixedPrice', headerText: 'Policy Price', width: 130, format: 'N4' },
                        { field: 'margin', headerText: 'Margin', width: 110, format: 'N4' },
                        { field: 'marginPercent', headerText: 'Margin %', width: 110, format: 'N2' },
                        {
                            field: 'hasPromotion', headerText: 'Promotion', width: 110,
                            template: '${if(hasPromotion)}<span class="badge bg-warning text-dark">Active</span>${else}-${/if}'
                        },
                        { field: 'promotionalPrice', headerText: 'Promo Price', width: 130, format: 'N4' },
                    ],
                    toolbar: ['ExcelExport', 'Search'],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['productNumber', 'productName', 'defaultPrice', 'weightedAverageCost', 'activePricePolicyName', 'activeFixedPrice', 'margin', 'marginPercent', 'hasPromotion']);
                    },
                    excelExportComplete: () => { },
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') { mainGrid.obj.excelExport(); }
                    }
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PriceReports']);
                await SecurityManager.validateToken();
                await methods.populateProductGroupListLookupData();
                await methods.populateProductListLookupData();
                await mainGrid.create([]);
                productGroupIdLookup.create();
                productIdLookup.create();
            } catch (e) {
            } finally {
            }
        });

        return {
            mainGridRef, productGroupIdRef, productIdRef, state, handler,
        };
    }
};

Vue.createApp(App).mount('#app');
