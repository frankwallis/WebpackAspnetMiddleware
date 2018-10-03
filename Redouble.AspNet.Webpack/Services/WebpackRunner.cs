using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Redouble.AspNet.Webpack
{
    public class WebpackRunner : BackgroundService
    {
        private IWebpackService _webpackService;

        public WebpackRunner(IWebpackService webpackService)
        {
             _webpackService = webpackService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _webpackService.ExecuteAsync(stoppingToken);
        }
    }
}
