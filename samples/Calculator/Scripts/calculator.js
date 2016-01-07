var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var React = require('react');
var calculator_store_1 = require("./calculator-store");
var Calculator = (function (_super) {
    __extends(Calculator, _super);
    function Calculator(props) {
        _super.call(this, props);
        this.calculatorStore = new calculator_store_1.CalculatorStore();
    }
    Calculator.prototype.input = function (digit) {
        this.calculatorStore.input(digit);
        this.forceUpdate();
    };
    Calculator.prototype.clear = function () {
        this.calculatorStore.clear();
        this.forceUpdate();
    };
    Calculator.prototype.add = function () {
        this.calculatorStore.add();
        this.forceUpdate();
    };
    Calculator.prototype.inputButton = function (digit) {
        var _this = this;
        return React.createElement("button", {"className": "adder-button adder-button-digit", "key": digit, "onClick": function () { return _this.input(digit); }}, digit);
    };
    Calculator.prototype.render = function () {
        var _this = this;
        // build the rows of digits
        var buttons = [
            [1, 2, 3].map(function (digit) { return _this.inputButton(digit); }),
            [4, 5, 6].map(function (digit) { return _this.inputButton(digit); }),
            [7, 8, 9].map(function (digit) { return _this.inputButton(digit); })
        ];
        // add the bottom row
        buttons.push([
            React.createElement("button", {"className": "adder-button adder-button-clear", "key": "clear", "onClick": function () { return _this.clear(); }}, "c"),
            this.inputButton(0),
            React.createElement("button", {"className": "adder-button adder-button-add", "key": "add", "onClick": function () { return _this.add(); }}, "+")
        ]);
        // wrap with row divs
        var buttonrows = buttons.map(function (row, idx) {
            return (React.createElement("div", {"key": "row" + idx, "className": "adder-row"}, row));
        });
        return (React.createElement("div", {"className": "adder-container"}, React.createElement("div", {"className": "adder-row"}, React.createElement("span", {"className": "adder-operand adder-display"}, this.calculatorStore.operand)), React.createElement("div", {"className": "adder-row"}, React.createElement("span", {"className": "adder-total adder-display"}, this.calculatorStore.total)), buttonrows));
    };
    return Calculator;
})(React.Component);
exports.Calculator = Calculator;
//# sourceMappingURL=calculator.js.map