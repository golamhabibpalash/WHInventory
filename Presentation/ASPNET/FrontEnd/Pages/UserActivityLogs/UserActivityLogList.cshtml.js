const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            filterActivityType: '',
            filterFromDate: '',
            filterToDate: ''
        });

        const mainGridRef = Vue.ref(null);

        const services = {
            getMainData: async (activityType, fromDate, toDate) => {
                let url = '/UserActivityLog/GetUserActivityLogList?';
                if (activityType) url += `activityType=${encodeURIComponent(activityType)}&`;
                if (fromDate) url += `fromDate=${encodeURIComponent(fromDate)}&`;
                if (toDate) url += `toDate=${encodeURIComponent(toDate + 'T23:59:59')}&`;
                return await AxiosManager.get(url, {});
            }
        };

        const methods = {
            populateMainData: async (activityType, fromDate, toDate) => {
                try {
                    const response = await services.getMainData(activityType, fromDate, toDate);
                    state.mainData = response?.data?.content?.data ?? [];
                    mainGrid.refresh();
                } catch (error) {
                }
            }
        };

        const handler = {
            applyFilter: async () => {
                await methods.populateMainData(state.filterActivityType, state.filterFromDate, state.filterToDate);
            },
            clearFilter: async () => {
                state.filterActivityType = '';
                state.filterFromDate = '';
                state.filterToDate = '';
                await methods.populateMainData();
            }
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '480px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'userEmail', headerText: 'User Email', width: 220, minWidth: 180 },
                        { field: 'activityType', headerText: 'Activity Type', width: 140, minWidth: 120 },
                        { field: 'description', headerText: 'Description', width: 300, minWidth: 200 },
                        { field: 'pageUrl', headerText: 'Page URL', width: 250, minWidth: 180 },
                        { field: 'ipAddress', headerText: 'IP Address', width: 130, minWidth: 100 },
                        { field: 'userAgent', headerText: 'User Agent', width: 200, minWidth: 150 },
                        { field: 'createdAtUtc', headerText: 'Timestamp (UTC)', width: 175, format: 'yyyy-MM-dd HH:mm:ss' }
                    ],
                    toolbar: ['ExcelExport', 'Search'],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['userEmail', 'activityType', 'description', 'pageUrl', 'ipAddress', 'createdAtUtc']);
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport({ fileName: `UserActivityLog_${new Date().toISOString().slice(0,10)}.xlsx` });
                        }
                    }
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        Vue.onMounted(async () => {
            SecurityManager.authorizePage(['UserActivityLogs']);
            mainGrid.create([]);
            await methods.populateMainData();
        });

        return { state, mainGridRef, handler };
    }
};

Vue.createApp(App).mount('#app');
