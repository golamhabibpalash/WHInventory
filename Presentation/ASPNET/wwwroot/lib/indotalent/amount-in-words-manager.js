const AmountInWordsManager = {
    _ones: [
        '', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine',
        'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen',
        'Seventeen', 'Eighteen', 'Nineteen'
    ],

    _tens: ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'],

    _belowHundred(n) {
        n = Math.floor(n);
        if (n < 20) return this._ones[n];
        return this._tens[Math.floor(n / 10)] + (n % 10 > 0 ? ' ' + this._ones[n % 10] : '');
    },

    _integer(n) {
        n = Math.floor(n);
        if (n === 0) return '';

        const parts = [];

        if (n >= 10000000) {
            parts.push(this._integer(Math.floor(n / 10000000)) + ' Crore');
            n = n % 10000000;
        }
        if (n >= 100000) {
            parts.push(this._integer(Math.floor(n / 100000)) + ' Lakh');
            n = n % 100000;
        }
        if (n >= 1000) {
            parts.push(this._integer(Math.floor(n / 1000)) + ' Thousand');
            n = n % 1000;
        }
        if (n >= 100) {
            parts.push(this._ones[Math.floor(n / 100)] + ' Hundred');
            n = n % 100;
        }
        if (n > 0) {
            parts.push(this._belowHundred(n));
        }

        return parts.join(' ');
    },

    convert(amount) {
        amount = parseFloat(amount);
        if (isNaN(amount) || amount < 0) return '';
        if (amount === 0) return 'Zero Taka Only';

        const taka = Math.floor(amount);
        const paisa = Math.round((amount - taka) * 100);

        const parts = [];
        if (taka > 0) parts.push(this._integer(taka) + ' Taka');
        if (paisa > 0) parts.push(this._belowHundred(paisa) + ' Paisa');

        return parts.join(' and ') + ' Only';
    },
};
