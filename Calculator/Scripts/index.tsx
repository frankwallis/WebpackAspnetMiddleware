import * as React from 'react';
import * as ReactDOM from 'react-dom';
import {Calculator} from './calculator';
import './calculator.css';

var container = document.getElementById('main');
ReactDOM.render(<Calculator />, container);

if (module.hot) {
   module.hot.accept(() => {
      ReactDOM.render(<Calculator />, container);
   });
}

declare var module: any;