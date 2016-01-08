using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System.Threading.Tasks;

namespace Redouble.AspNet.Webpack
{
    public class WebpackDevServer
    {
        RequestDelegate _next;
        IWebpackService _webpackService;
                
        public WebpackDevServer(RequestDelegate next, IWebpackService webpackService)
        {
            _next = next;
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