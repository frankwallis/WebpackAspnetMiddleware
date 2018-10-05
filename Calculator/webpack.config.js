var path = require('path');
var webpack = require('webpack');
var WebpackNotifierPlugin = require('webpack-notifier');

module.exports = function (env) {
  return {
    mode: env,
    context: path.join(__dirname, 'Scripts'),
    entry: ['react-hot-loader/patch', 'webpack-aspnet-middleware/client', './index'],
    devtool: (env === 'development') ? 'cheap-module-eval-source-map' : 'source-map',
    resolve: {
      extensions: ['.ts', '.tsx', '.js', '.jsx']
    },
    output: {
      publicPath: '/webpack/',
      path: path.join(__dirname, 'wwwroot', 'webpack'),
      filename: '[name].bundle.js'
    },
    plugins: [
      new WebpackNotifierPlugin(),
      new webpack.HotModuleReplacementPlugin()
    ],
    module: {
      rules: [
        { test: /\.tsx?$/, use: ['react-hot-loader/webpack', 'awesome-typescript-loader'], exclude: /node_modules/ },
        { test: /\.css$/, use: ['style-loader', 'css-loader'] }
      ]
    }
  };
}
