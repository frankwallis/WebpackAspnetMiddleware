using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace Redouble.AspNet.Webpack
{
    public class WebpackOptions
    {
        /* path of webpack configuration file */
        public string ConfigFile { get; set; }
        /* matches 'output.publicPath' in webpack config file */
        public string PublicPath { get; set; }
        /* location of web root directory relative to project directory */
        public string WebRoot { get; set; }
    }

    public interface IWebpackService
    {
        event EventHandler<JToken> Valid;
        event EventHandler Invalid;
        Task<IWebpackFile> GetFile(string filename);
        bool IsWebpackFile(string filename);
    }

    public class WebpackService : IWebpackService
    {
        private IHostingEnvironment _environment;
        private ILogger _logger;
        private WebpackOptions _options;

        public WebpackService(IHostingEnvironment environment,
            ILogger<WebpackService> logger,
            WebpackOptions options)
        {
            _environment = environment;
            _logger = logger;
            _options = options;
            _host = CreateHost(_environment.ContentRootPath);
        }

        private Task<NodeHost> _host;

        private async Task<NodeHost> CreateHost(string basePath)
        {
            var host = NodeHost.Create("webpack-aspnet-middleware", basePath);
            host.Emit += WebpackEmit;
            await host.Invoke("start", Path.Combine(basePath, _options.ConfigFile), null);
            return host;
        }

        private void WebpackEmit(object sender, EmitEventArgs e)
        {
            if (e.Name == "invalid")
                OnInvalid();
            else if (e.Name == "valid")
                OnValid(e.Args);
            else
                throw new NotSupportedException("Unrecognised webpack event [" + e.Name + "]");
        }

        public async Task<IWebpackFile> GetFile(string filename)
        {
            var host = await _host;
            var absolutePath = Path.Combine(_environment.ContentRootPath, _options.WebRoot, filename.Substring(1));
            var webpackFile = await host.Invoke<WebpackFile>("getFile", absolutePath);
            return webpackFile;
        }

        public bool IsWebpackFile(string filename)
        {
            return (filename.IndexOf(_options.PublicPath) == 0);
        }

        public void OnValid(JToken args)
        {
            _logger.LogInformation("{0}  Bundle is now valid", "\u2705");

            if (Valid != null)
            {
                Valid(this, args);
            }
        }
        public void OnInvalid()
        {
            _logger.LogWarning("{0}  Bundle is invalid", "\u274C");

            if (Invalid != null)
            {
                Invalid(this, EventArgs.Empty);
            }
        }

        public event EventHandler<JToken> Valid;
        public event EventHandler Invalid;
    }

    public interface IWebpackFile
    {
        string Contents { get; }
        string MimeType { get; }
    }

    public class WebpackFile : IWebpackFile
    {
        public WebpackFile(string contents, string mimeType)
        {
            Contents = contents;
            MimeType = mimeType;
        }

        public string Contents { get; private set; }
        public string MimeType { get; private set; }
    }
}