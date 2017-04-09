import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { AppContainer } from 'react-hot-loader'
import { Calculator } from './calculator'
import './calculator.css'

const main = document.getElementById('main')
const render = (App) => ReactDOM.render(<AppContainer><App /></AppContainer>, main)
render(Calculator)

if (module.hot) {
    module.hot.accept('./calculator', () => {
        const NextCalculator = require<any>('./calculator').Calculator
        render(NextCalculator)
    })
    // TODO - output with module="es2015"
    // module.hot.accept('./calculator', () => render(Calculator))
}