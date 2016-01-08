# Webpack Aspnet Middleware

Aren't ya just sick of those NodeJS guys with their fancy hot module reloading and their la-di-da ways. 
Now you can be as smug as them by using WebpackAspnetMiddleware!

# Overview

# Instructions
1) Install the [Redouble.AspNet.Webpack](https://www.nuget.org/packages/Redouble.Aspnet.Webpack/) NuGet package:
```
  Windows> Install-Package Redouble.Aspnet.Webpack
```
```
  macbook> dnx install Redouble.Aspnet.Webpack
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

4) Optional: configure hot-reloading in webpack
...

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
