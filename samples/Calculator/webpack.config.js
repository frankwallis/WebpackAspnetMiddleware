var path = require('path');
var WebpackNotifierPlugin = require('webpack-notifier');
var webpack = require('webpack');

module.exports = {
    context: path.join(__dirname, 'Scripts'),
    entry: [ 'webpack-aspnet-middleware/client', './index' ],
    devtool: 'source-map',
    resolve: {
        extensions: ['', '.webpack.js', '.web.js', '.js']
    },
    output: {
        publicPath: "/webpack/",
        path: path.join(__dirname, 'wwwroot', 'webpack'),
        filename: '[name].bundle.js'
    },
    plugins: [
        new WebpackNotifierPlugin(),
        new webpack.HotModuleReplacementPlugin()
    ],
    module: {
        loaders: [
            { test: /\.css$/, loader: "style!css" }
        ]
    }  
};