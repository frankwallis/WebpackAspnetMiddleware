import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { AppContainer } from 'react-hot-loader'
import { Calculator } from './calculator'
import './calculator.css'

const main = document.getElementById('main')
const render = () => ReactDOM.render(<AppContainer><Calculator /></AppContainer>, main)
render()

if (module.hot) {
    module.hot.accept('./calculator', render);
}

declare const module: any