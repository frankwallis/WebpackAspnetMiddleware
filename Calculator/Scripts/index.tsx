import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { AppContainer } from 'react-hot-loader'
import { Calculator } from './calculator'

let main = document.getElementById('main')
const render = (App) => ReactDOM.render(<AppContainer><App /></AppContainer>, main)
render(Calculator)

module.hot && module.hot.accept('./calculator', () => render(Calculator))