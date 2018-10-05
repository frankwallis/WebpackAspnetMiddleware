using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Redouble.AspNet.Webpack
{
    public class WebpackRunner : IHostedService
    {
        private IWebpackService _webpackService;
        private CancellationTokenSource _stoppingTokenSource;

        public WebpackRunner(IWebpackService webpackService)
        {
             _webpackService = webpackService;
             _stoppingTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken token)
        {
            return _webpackService.Start(_stoppingTokenSource.Token);
        }

        public Task StopAsync(CancellationToken token)
        {
            _stoppingTokenSource.Cancel();
            return _webpackService.Stopped;
        }
    }
}
