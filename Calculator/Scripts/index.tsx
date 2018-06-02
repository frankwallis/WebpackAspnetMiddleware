import React from 'react'
import ReactDOM from 'react-dom'
import { AppContainer } from 'react-hot-loader'
import { Calculator } from './calculator'

const main = document.getElementById('main')
const render = (App) => ReactDOM.render(<AppContainer><App /></AppContainer>, main)
render(Calculator)

module.hot && module.hot.accept('./calculator', () => render(Calculator))