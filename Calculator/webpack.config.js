var path = require('path');
var WebpackNotifierPlugin = require('webpack-notifier');
var webpack = require('webpack');

module.exports = {
   context: path.join(__dirname, 'Scripts'),
   entry: ['webpack-aspnet-middleware/client', './index'],
   devtool: 'source-map',
   resolve: {
      extensions: ['', '.ts', '.tsx', '.js', '.jsx']
   },
   output: {
      publicPath: "/webpack/",
      path: path.join(__dirname, 'wwwroot', 'webpack'),
      filename: '[name].bundle.js'
   },
   plugins: [
      new WebpackNotifierPlugin(),
      new webpack.optimize.OccurenceOrderPlugin(), new webpack.optimize.OccurenceOrderPlugin(),
      new webpack.HotModuleReplacementPlugin()
   ],
   module: {
      loaders: [
         { test: /\.css$/, loader: "style!css" },
         {
            test: /\.tsx?$/,
            exclude: /node_modules/,
            loader: 'babel',
            query: {
               "presets": [
                  "es2015",
                  "react",
                  "react-hmre"
               ]
            }
         },
         { test: /\.tsx?$/, loader: "ts", exclude: /node_modules/ }
      ]
   }
};