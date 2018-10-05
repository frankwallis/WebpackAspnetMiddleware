import React from 'react'
import * as CalculatorStore from './calculator-store'
import './calculator.css'

export class Calculator extends React.Component<{}, CalculatorStore.CalculatorState> {
  constructor(props) {
    super(props)
    this.state = CalculatorStore.clear()
  }

  inputButton(digit: number) {
    return (
      <button className="adder-button adder-button-digit"
        key={digit}
        onClick={() => this.setState(CalculatorStore.input(digit))}>{digit}</button>
    )
  }

  render() {
    // build the rows of digits
    const buttons = [
      // UNCOMMENT ME! 
      // [1, 2, 3].map((digit) => this.inputButton(digit)),
      [4, 5, 6].map((digit) => this.inputButton(digit)),
      [7, 8, 9].map((digit) => this.inputButton(digit))
    ]

    // add the bottom row
    buttons.push([
      <button className="adder-button adder-button-clear"
        key="clear"
        onClick={() => this.setState(CalculatorStore.clear)}>c</button>,
      this.inputButton(0),
      <button className="adder-button adder-button-add"
        key="add"
        onClick={() => this.setState(CalculatorStore.sum)}>+</button>
    ])

    // wrap with row divs
    const buttonrows = buttons.map((row, idx) => {
      return (
        <div key={"row" + idx} className="adder-row">
          {row}
        </div>
      )
    })

    return (
      <div className="adder-container">
        <div className="adder-row">
          <span className="adder-operand adder-display">{this.state.operand}</span>
        </div>

        <div className="adder-row">
          <span className="adder-total adder-display">{this.state.total}</span>
        </div>

        {buttonrows}
      </div>
    )
  }
}
