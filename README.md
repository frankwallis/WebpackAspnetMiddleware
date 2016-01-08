# Webpack Aspnet Middleware

ASP.NET 5 Middleware providing a development file server and hot module reloading for applications built with [Webpack](https://github.com/webpack/webpack)

# Overview

WebpackAspnetMiddleware is an ASP.NET clone of the popular [webpack-dev-middleware](https://github.com/webpack/webpack-dev-middleware.git) and [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware.git) NodeJS projects. It uses the ASP.NET [NodeServices](https://github.com/aspnet/NodeServices.git) package to start a NodeJS instance running webpack. The development server middleware serves up the files produced by webopack and the hot reload middleware notifies the client when they change. In the browser the [webpack-hot-middleware](https://github.com/glenjamin/webpack-hot-middleware.git) client library is used with no changes.

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
    services.AddWebpack("webpack.config.js", "/webpack/"); // needed
    services.AddMvc();
  }

  public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
  {
    app.UseWebpackDevServer();                            // needed
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
cd samples/Calculator
npm install
dnu restore
dnu build
dnx web
```
