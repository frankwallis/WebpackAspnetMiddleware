using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Redouble.AspNet.Webpack
{
    public class DevServer
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private ILogger _logger;

        public DevServer(RequestDelegate next,
            IWebpackService webpackService,
            ILogger<DevServer> logger)
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
            try
            {
                var file = await _webpackService.GetFile(context.Request.Path);

                /* set some headers */
                context.Response.ContentLength = file.Contents.Length;
                context.Response.ContentType = file.MimeType;

                /* write contents to response body */
                await context.Response.Body.WriteAsync(file.Contents, 0, file.Contents.Length);
            }
            catch
            {
                _logger.LogWarning("File {0} not found", context.Request.Path);
                context.Response.StatusCode = 404;
            }
        }
    }
}