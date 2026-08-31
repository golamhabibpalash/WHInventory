const DateFormatManager = {
    // The application shows dates as DD/MM/YYYY everywhere. en-GB is the locale whose short
    // date is exactly that, so the browser does the work and the separator stays consistent.
    formatToLocale: (date) => {

        const formatter = new Intl.DateTimeFormat('en-GB', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
        });
        const newDate = new Date(date)
        return formatter.format(newDate);
    },

    // yyyy-MM-dd for API payloads. Built from the local parts rather than toISOString(), which
    // converts to UTC first and so sends the previous day from any timezone ahead of it.
    toApiDate: (date) => {
        if (!date) return null;
        const value = date instanceof Date ? date : new Date(date);
        if (isNaN(value.getTime())) return null;
        const pad = (n) => String(n).padStart(2, '0');
        return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
    },

    // The inverse: a yyyy-MM-dd string back to a Date at local midnight. new Date('2026-08-31')
    // would read it as UTC midnight, which is the previous day anywhere behind UTC.
    fromApiDate: (value) => {
        if (!value) return null;
        if (value instanceof Date) return value;
        const parts = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(value));
        if (parts) {
            return new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3]));
        }
        const parsed = new Date(value);
        return isNaN(parsed.getTime()) ? null : parsed;
    },

};
