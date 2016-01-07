using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Redouble.Aspnet.Webpack;

namespace Calculator
{
    public class Startup
    {
        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                  .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                  .UseStartup<Startup>()
                  .Build();

            application.Run();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebpack("webpack.config.js", "/webpack");
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
