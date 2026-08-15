const App = {
    setup() {
        const STATUS_COLORS = {
            'In Stock': '#3b82f6',
            'Reserved': '#8b5cf6',
            'In Transit': '#f59e0b',
            'On Order': '#10b981'
        };
        const FALLBACK_COLOR = '#94a3b8';

        const state = Vue.reactive({
            todayLabel: '',
            kpi: {
                totalInventory: 0,
                totalInventoryDeltaPct: null,
                inboundToday: 0,
                inboundDeltaPct: null,
                outboundToday: 0,
                outboundDeltaPct: null,
                lowStockCount: 0,
                lowStockDeltaPct: null,
                lowStockThreshold: 0
            },
            inventoryStatus: [],
            statusTotal: 0,
            movementTrend: [],
            topCategories: [],
            recentActivities: []
        });

        const cardSalesQtyRef = Vue.ref(null);
        const cardSalesReturnQtyRef = Vue.ref(null);
        const cardPurchaseQtyRef = Vue.ref(null);
        const cardPurchaseReturnQtyRef = Vue.ref(null);
        const cardDeliveryOrderQtyRef = Vue.ref(null);
        const cardGoodsReceiveQtyRef = Vue.ref(null);
        const cardTransferOutQtyRef = Vue.ref(null);
        const cardTransferInQtyRef = Vue.ref(null);

        const statusChartRef = Vue.ref(null);
        const trendChartRef = Vue.ref(null);

        const services = {
            getCardsData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetCardsDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getOverviewData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetOverviewDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            formatQty: (value) => {
                const number = Number(value) || 0;
                return number.toLocaleString(undefined, { maximumFractionDigits: 0 });
            },
            formatDelta: (value) => {
                if (value === null || value === undefined) return 'n/a';
                const sign = value > 0 ? '+' : '';
                return `${sign}${Number(value).toFixed(1)}%`;
            },
            // A rise is good by default; pass invert for metrics where a rise is bad (low stock).
            deltaClass: (value, invert) => {
                if (value === null || value === undefined || value === 0) return 'flat';
                const positive = invert ? value < 0 : value > 0;
                return positive ? 'up' : 'down';
            },
            deltaIcon: (value) => {
                if (value === null || value === undefined || value === 0) return 'fas fa-minus';
                return value > 0 ? 'fas fa-arrow-up' : 'fas fa-arrow-down';
            },
            formatTime: (value) => {
                if (!value) return '';
                const date = new Date(value);
                if (isNaN(date.getTime())) return '';
                return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
            },
            populateCardsData: async () => {
                const response = await services.getCardsData();
                const cardsDashboard = response?.data?.content?.data?.cardsDashboard;

                if (!cardsDashboard) {
                    return;
                }

                const countUp = (el, end) => {
                    if (!el) return;
                    const target = parseInt(end) || 0;
                    const duration = 1000;
                    const t0 = performance.now();
                    const tick = (now) => {
                        const p = Math.min((now - t0) / duration, 1);
                        const eased = 1 - Math.pow(1 - p, 3);
                        el.textContent = Math.round(eased * target).toLocaleString();
                        if (p < 1) requestAnimationFrame(tick);
                    };
                    requestAnimationFrame(tick);
                };

                countUp(cardSalesQtyRef.value, cardsDashboard.salesTotal);
                countUp(cardSalesReturnQtyRef.value, cardsDashboard.salesReturnTotal);
                countUp(cardPurchaseQtyRef.value, cardsDashboard.purchaseTotal);
                countUp(cardPurchaseReturnQtyRef.value, cardsDashboard.purchaseReturnTotal);
                countUp(cardDeliveryOrderQtyRef.value, cardsDashboard.deliveryOrderTotal);
                countUp(cardGoodsReceiveQtyRef.value, cardsDashboard.goodsReceiveTotal);
                countUp(cardTransferOutQtyRef.value, cardsDashboard.transferOutTotal);
                countUp(cardTransferInQtyRef.value, cardsDashboard.transferInTotal);
            },
            populateOverviewData: async () => {
                const response = await services.getOverviewData();
                const data = response?.data?.content?.data;

                if (!data) {
                    return;
                }

                state.kpi = data.kpiDashboard ?? state.kpi;
                state.movementTrend = data.movementTrendDashboard ?? [];
                state.topCategories = data.topCategoryDashboard ?? [];

                const slices = (data.inventoryStatusDashboard ?? []).filter(x => (x.value ?? 0) > 0);
                const total = slices.reduce((sum, x) => sum + (x.value ?? 0), 0);
                state.statusTotal = total;
                state.inventoryStatus = slices.map(x => ({
                    label: x.label,
                    value: x.value,
                    color: STATUS_COLORS[x.label] ?? FALLBACK_COLOR,
                    percentage: total > 0 ? Math.round((x.value / total) * 1000) / 10 : 0
                }));

                state.recentActivities = (data.recentActivityDashboard ?? []).map((x, index) => ({
                    key: `${x.number}-${index}`,
                    title: x.title,
                    number: x.number,
                    direction: x.direction,
                    quantity: x.quantity,
                    timeLabel: methods.formatTime(x.occurredAtUtc)
                }));

                methods.populateStatusChart();
                methods.populateTrendChart();
            },
            populateStatusChart: () => {
                if (!statusChartRef.value) return;

                new ej.charts.AccumulationChart({
                    series: [{
                        // Syncfusion mutates its data source, so hand it a plain array not a Vue proxy.
                        dataSource: Vue.toRaw(state.inventoryStatus).map(x => ({ ...x })),
                        xName: 'label',
                        yName: 'value',
                        pointColorMapping: 'color',
                        innerRadius: '70%',
                        radius: '92%',
                        border: { width: 3, color: '#ffffff' },
                        dataLabel: { visible: false }
                    }],
                    legendSettings: { visible: false },
                    tooltip: { enable: true, format: '${point.x}: <b>${point.y}</b>' },
                    enableAnimation: true,
                    background: 'transparent',
                    height: '200px',
                    width: '200px'
                }, statusChartRef.value);
            },
            populateTrendChart: () => {
                if (!trendChartRef.value) return;

                const axisLabelStyle = { color: '#94a3b8', size: '11px' };
                const trendPoints = Vue.toRaw(state.movementTrend).map(x => ({ ...x }));
                const marker = (color) => ({
                    visible: true,
                    width: 7,
                    height: 7,
                    fill: color,
                    border: { width: 2, color: '#ffffff' }
                });

                new ej.charts.Chart({
                    primaryXAxis: {
                        valueType: 'Category',
                        interval: 1,
                        majorGridLines: { width: 0 },
                        majorTickLines: { width: 0 },
                        lineStyle: { width: 1, color: '#e2e8f0' },
                        labelStyle: axisLabelStyle,
                        labelIntersectAction: 'Rotate45'
                    },
                    primaryYAxis: {
                        majorTickLines: { width: 0 },
                        lineStyle: { width: 0 },
                        majorGridLines: { width: 1, color: '#f1f5f9' },
                        labelStyle: axisLabelStyle
                    },
                    chartArea: { border: { width: 0 } },
                    series: [
                        {
                            type: 'SplineArea',
                            dataSource: trendPoints,
                            xName: 'label',
                            yName: 'inbound',
                            name: 'Inbound',
                            fill: 'rgba(16,185,129,.10)',
                            border: { width: 2.5, color: '#10b981' },
                            marker: marker('#10b981')
                        },
                        {
                            type: 'SplineArea',
                            dataSource: trendPoints,
                            xName: 'label',
                            yName: 'outbound',
                            name: 'Outbound',
                            fill: 'rgba(249,115,22,.10)',
                            border: { width: 2.5, color: '#f97316' },
                            marker: marker('#f97316')
                        }
                    ],
                    legendSettings: {
                        visible: true,
                        position: 'Top',
                        alignment: 'Near',
                        shapeHeight: 9,
                        shapeWidth: 9,
                        textStyle: { color: '#475569', size: '12px' }
                    },
                    tooltip: { enable: true, shared: true },
                    background: 'transparent',
                    height: '270px'
                }, trendChartRef.value);
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Dashboards']);
                await SecurityManager.validateToken();

                state.todayLabel = new Date().toLocaleDateString('en-US', {
                    weekday: 'short', year: 'numeric', month: 'short', day: 'numeric'
                });

                await Promise.all([
                    methods.populateCardsData(),
                    methods.populateOverviewData(),
                ]);

            } catch (e) {
            }
        });

        return {
            cardSalesQtyRef,
            cardSalesReturnQtyRef,
            cardPurchaseQtyRef,
            cardPurchaseReturnQtyRef,
            cardDeliveryOrderQtyRef,
            cardGoodsReceiveQtyRef,
            cardTransferOutQtyRef,
            cardTransferInQtyRef,
            statusChartRef,
            trendChartRef,
            state,
            methods
        };
    }
};

Vue.createApp(App).mount('#app');
