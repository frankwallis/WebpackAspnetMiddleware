# Webpack Aspnet Middleware

Development file server and hot module reloading middleware for applications built with [Webpack](https://github.com/webpack/webpack) and running in ASP.NET 5

[![build status](https://secure.travis-ci.org/frankwallis/WebpackAspnetMiddleware.png?branch=master)](http://travis-ci.org/frankwallis/WebpackAspnetMiddleware)

For **aspnet@1.0.0-rc1**, use WebpackAspnetMiddleware@0.7.9  
For **aspnet@1.0.0-rc2**, use WebpackAspnetMiddleware@0.8.x  

# Overview

WebpackAspnetMiddleware is an ASP.NET clone of the popular [webpack-dev-middleware](https://github.com/webpack/webpack-dev-middleware.git) and [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware) NodeJS packages. It come in 3 parts: The **WebpackService**, the **DevServer** middleware and the **HotReload** middleware.

The WebpackService starts NodeJS using the [JavaScriptServices](https://github.com/aspnet/JavaScriptServices) package, and then sets up a two-way asynchronous communication channel with the node instance. It comes with an accompanying npm package [webpack-aspnet-middleware](https://www.npmjs.com/package/webpack-aspnet-middleware) which starts webpack to generate the bundles, and notifies the WebpackService when files are emitted.

The DevServer middleware serves up the files produced by the webpack instance, and the HotReload middleware notifies the client when files change. In the browser the [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware) client library is used with no changes.

# Instructions
1) Add the [Redouble.AspNet.Webpack](https://www.nuget.org/packages/Redouble.Aspnet.Webpack/) NuGet package to your dependencies:
```js
   "dependencies": {
      "Microsoft.NETCore.App": {
         "version": "1.0.0-rc2-*",
         "type": "platform"
      },
      "Microsoft.AspNetCore.Mvc": "1.0.0-rc2-final",
      "Microsoft.AspNetCore.Server.Kestrel": "1.0.0-rc2-final",
      "Redouble.AspNet.Webpack": "0.8.1"
   },
```

2) Install the [webpack-aspnet-middleware](https://www.npmjs.com/package/webpack-aspnet-middleware) NodeJS package:
```sh
  npm install webpack-aspnet-middleware --save-dev
```

3) Add the necessary services and middleware to your ASP.NET startup module:
```cs
  public void ConfigureServices(IServiceCollection services)
  {    
     /* these are the default values */   
     services.AddWebpack(
        configFile: "webpack.config.js",      // relative to project directory
        publicPath: "/webpack/",              // should match output.publicPath in your webpack config
        webRoot: "./wwwroot",                 // relative to project directory
        logLevel: WebpackLogLevel.Normal      // None, ErrorsOnly, Minimal, Normal or Verbose
     );       
  }

  public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
  {
     app.UseWebpackDevServer();               // necessary
     app.UseWebpackHotReload();               // optional

     app.UseStaticFiles();
     app.UseMvc(routes => routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}"));
  }
```

4) *Optional:* configure hot-reloading in your webpack configuration file:

a) Add these to the ```plugins``` array:
```js
  plugins: [
     new webpack.optimize.OccurenceOrderPlugin(),
     new webpack.HotModuleReplacementPlugin(),
     new webpack.NoErrorsPlugin()
  ]
```
b) Add the hot reload client to the ```entry``` array:
```js
  entry: [ 'webpack-aspnet-middleware/client', './index' ],
```
5) Start ASP.NET:
```sh
  macbook> dotnet run
```

# Samples

```sh
git clone https://github.com/frankwallis/WebpackAspnetMiddleware
cd WebpackAspnetMiddleware
dotnet restore
cd Calculator
npm install
dotnet run
```
Open up [localhost:5000](http://localhost:5000) in your browser and then try editing ```samples/Calculator/Scripts/calculator.tsx```.

# Troubleshooting

First verify that webpack works when run from the command line.
