export interface CalculatorState {
  operand: number
  total: number
}

export function clear() {
  return {
    operand: 0.0,
    total: 0.0
  }
}

export function sum(state: CalculatorState) {
  return {
    total: state.total + state.operand,
    operand: 0.0
  }
}

export function input(digit: number) {
  return (state: CalculatorState) => {
    return { operand: (state.operand * 10) + digit }
  }
}
