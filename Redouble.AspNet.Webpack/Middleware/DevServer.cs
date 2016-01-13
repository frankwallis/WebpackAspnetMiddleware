using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Redouble.AspNet.Webpack
{
    public class WebpackDevServer
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private ILogger _logger;

        public WebpackDevServer(RequestDelegate next,
            IWebpackService webpackService,
            ILogger<WebpackDevServer> logger)
        {
            _next = next;
            _logger = logger;
            _webpackService = webpackService;
        }

        public async Task Invoke(HttpContext context)
        {
            /* filter out our requests */
            if (context.Request.Method != "GET")
            {
                await _next(context);
                return;
            }

            if (!_webpackService.IsWebpackFile(context.Request.Path))
            {
                await _next(context);
                return;
            }

            _logger.LogDebug("Handling request for {0}", context.Request.Path);

            /* get the file details */
            var file = await _webpackService.GetFile(context.Request.Path);

            /* set some headers */
            context.Response.ContentLength = file.Contents.Length;
            context.Response.ContentType = file.MimeType;

            /* write contents to response body */
            await context.Response.WriteAsync(file.Contents);
        }
    }
}