using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Redouble.AspNet.Webpack;

namespace Calculator
{
    public class Startup
    {
        public static void Main(string[] args)
        {
            var application = new WebHostBuilder()
               .UseContentRoot(Directory.GetCurrentDirectory())
                  //.UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                  //.UseIISIntegration()
                  .UseKestrel()
                  .UseStartup<Startup>()
                  .Build();

            application.Run();
        }
            
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            System.Console.WriteLine(env.ContentRootPath);
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);
                //.AddEnvironmentVariables();
                //.AddJsonFile("appsettings.json")
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebpack(configFile: "webpack.config.js", publicPath: "/webpack/");
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Debug);
            loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Information);
            
            app.UseWebpackDevServer();
            app.UseWebpackHotReload();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
