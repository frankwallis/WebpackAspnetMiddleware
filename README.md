# Webpack Aspnet Middleware

ASP.NET 5 Middleware providing a development file server and hot module reloading for applications built with [Webpack](https://github.com/webpack/webpack)

[![build status](https://secure.travis-ci.org/frankwallis/WebpackAspnetMiddleware.png?branch=master)](http://travis-ci.org/frankwallis/WebpackAspnetMiddleware)

# Overview

WebpackAspnetMiddleware is an ASP.NET clone of the popular [webpack-dev-middleware](https://github.com/webpack/webpack-dev-middleware.git) and [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware.git) NodeJS packages. 

The middleware starts a NodeJS instance running webpack using the ASP.NET [NodeServices](https://github.com/aspnet/NodeServices.git) package. The development server middleware serves up the files produced by the webpack instance, and the hot reload middleware notifies the client when files change. In the browser the [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware.git) client library is used with no changes.

# Instructions
1) Install the [Redouble.AspNet.Webpack](https://www.nuget.org/packages/Redouble.Aspnet.Webpack/) NuGet package:
```
  Windows> Install-Package Redouble.AspNet.Webpack
```
```
  macbook> dnx install Redouble.AspNet.Webpack
```

2) Install the [webpack-aspnet-middleware](https://www.npmjs.com/package/webpack-aspnet-middleware) NodeJS package:
```
  npm install webpack-aspnet-middleware --save-dev
```

3) Add the necessary services and middleware to your ASP.NET startup module:
```
  public void ConfigureServices(IServiceCollection services)
  {    
    /* these are the default values */   
    services.AddWebpack(
       configFile: "webpack.config.js",      // relative to project directory
       publicPath: "/webpack/",              // should match output.publicPath in your webpack config
       webRoot: "./wwwroot"                  // relative to project directory
    );       
  }

  public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
  {
    app.UseWebpackDevServer();                            // necessary
    app.UseWebpackHotReload();                            // optional

    app.UseStaticFiles();
    app.UseMvc(routes => routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}"));
  }
```

4) Optional: configure hot-reloading in your webpack configuration file:

a) Add these to the ```plugins``` array:
```
  plugins: [
      new webpack.optimize.OccurenceOrderPlugin(),
      new webpack.HotModuleReplacementPlugin(),
      new webpack.NoErrorsPlugin()
  ]
```
b) Add the hot reload client to the ```entry``` array:
```
  entry: [ 'webpack-aspnet-middleware/client', './index' ],
```
5) Start ASP.NET
```
  macbook> dnx web
```

# Samples

```
git clone https://github.com/frankwallis/WebpackAspnetMiddleware
cd WebpackAspnetMiddleware
dnu restore
cd samples/Calculator
npm install
dnx web
```
Open up http://localhost:5000 in your browser and then try editing samples/Calculator/Scripts/calculator.tsx.

# Troubleshooting

First verify that webpack works when run from the command line.
