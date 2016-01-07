var CalculatorStore = (function () {
    function CalculatorStore() {
        this.clear();
    }
    CalculatorStore.prototype.input = function (digit) {
        this.operand = (this.operand * 10) + digit;
    };
    CalculatorStore.prototype.add = function () {
        this.total = this.total + this.operand;
        this.operand = 0.0;
    };
    CalculatorStore.prototype.clear = function () {
        this.total = 0.0;
        this.operand = 0.0;
    };
    return CalculatorStore;
})();
exports.CalculatorStore = CalculatorStore;
//# sourceMappingURL=calculator-store.js.map