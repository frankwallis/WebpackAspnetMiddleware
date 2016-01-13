using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Linq;

namespace Redouble.AspNet.Webpack
{
    public class WebpackOptions
    {
        /* path of webpack configuration file */
        public string ConfigPath { get; set; }
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
        private IApplicationEnvironment _environment;
        private WebpackOptions _options;

        public WebpackService(IApplicationEnvironment environment, WebpackOptions options)
        {
            _environment = environment;
            _options = options;
            _host = CreateHost(_environment.ApplicationBasePath);
        }

        private Task<NodeHost> _host;

        private async Task<NodeHost> CreateHost(string basePath)
        {
            var host = NodeHost.Create("webpack-aspnet-middleware", basePath);
            host.Emit += WebpackEmit;
            await host.Invoke("start", Path.Combine(basePath, _options.ConfigPath), null);
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
            var absolutePath = Path.Combine(_environment.ApplicationBasePath, _options.WebRoot, filename.Substring(1));
            var webpackFile = await host.Invoke<WebpackFile>("getFile", absolutePath);
            return webpackFile;
        }

        public bool IsWebpackFile(string filename)
        {
            return (filename.IndexOf(_options.PublicPath) == 0);
        }

        public void OnValid(JToken args)
        {
            if (Valid != null)
            {
                Valid(this, args);
            }
        }
        public void OnInvalid()
        {
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