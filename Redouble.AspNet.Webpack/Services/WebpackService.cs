using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Redouble.AspNet.Webpack
{
    public enum WebpackLogLevel { None, ErrorsOnly, Minimal, Normal, Verbose }

    public class WebpackOptions
    {
        /* path of webpack configuration file */
        public string ConfigFile { get; set; }
        /* matches 'output.publicPath' in webpack config file */
        public string PublicPath { get; set; }
        /* location of web root directory relative to project directory */
        public string WebRoot { get; set; }
        /* controls output from webpack */
        public WebpackLogLevel LogLevel { get; set; }
        /* the 'env' parameter passed to webpack.config.js, if not set it will default to the name of the environment */
        public object EnvParam { get; set; }
        /* controls frequency of heartbeats useful for testing */
        public int Heartbeat { get; set; }
    }

    public interface IWebpackService
    {
        event EventHandler<JToken> Valid;
        event EventHandler Invalid;
        Task<IWebpackFile> GetFile(string filename);
        bool IsWebpackFile(string filename);
        WebpackOptions Options { get; }
        Task Start(CancellationToken stoppingToken);
        Task Stopped { get; }
    }

    public class WebpackService : IWebpackService
    {
        private IHostingEnvironment _environment;
        private ILogger _logger;
        private WebpackOptions _options;
        private NodeHost _host;

        public WebpackService(
            IHostingEnvironment environment,
            ILogger<WebpackService> logger,
            WebpackOptions options)
        {
            _environment = environment;
            _logger = logger;
            _options = options;            
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            var environmentVariables = new Dictionary<string, string>();
            environmentVariables["NODE_ENV"] = _environment.EnvironmentName.ToLower();

            var startParams = new {                
                LogLevel = _options.LogLevel,
                EnvParam = _options.EnvParam ?? _environment.EnvironmentName.ToLower()
            };

            _host = await NodeHost.Create("webpack-aspnet-middleware", _environment.ContentRootPath, stoppingToken, _logger, environmentVariables);
            _host.Emit += WebpackEmit;
            
            await _host.Invoke("start", Path.Combine(_environment.ContentRootPath, _options.ConfigFile), startParams.LogLevel);
        }

        public Task Stopped
        {
            get
            {
                return _host?.Stopped;
            }
        }

        private void WebpackEmit(object sender, EmitEventArgs e)
        {
            if (e.Name == "invalid")
                OnInvalid();
            else if (e.Name == "valid")
                OnValid(e.Args);
            else if (e.Name == "log")
                OnLog(e.Args);
            else
                throw new NotSupportedException("Unrecognised webpack event [" + e.Name + "]");
        }

        public WebpackOptions Options
        {
            get { return _options; }
        }

        public async Task<IWebpackFile> GetFile(string filename)
        {            
            if (_host == null)
            {
                throw new Exception("WebpackService has not been started");
            }

            var absolutePath = Path.Combine(_environment.ContentRootPath, _options.WebRoot, filename.Substring(1));
            var webpackFile = await _host.Invoke<WebpackFile>("getFile", absolutePath);
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

        public void OnLog(JToken args)
        {
            _logger.LogInformation(args.ToString());
        }

        public event EventHandler<JToken> Valid;
        public event EventHandler Invalid;
    }

    public interface IWebpackFile
    {
        byte[] Contents { get; }
        string MimeType { get; }
    }

    public class WebpackFile : IWebpackFile
    {
        public WebpackFile(byte[] contents, string mimeType)
        {
            Contents = contents;
            MimeType = mimeType;
        }

        public byte[] Contents { get; private set; }
        public string MimeType { get; private set; }
    }
}
