using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Redouble.AspNet.Webpack
{
    public class WebpackHotReload
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private ILogger _logger;
        private Timer _heartbeatTimer;

        public WebpackHotReload(RequestDelegate next,
            IWebpackService webpackService,
            ILogger<WebpackHotReload> logger)
        {
            _next = next;
            _logger = logger;

            _webpackService = webpackService;
            _webpackService.Valid += WebpackValid;
            _webpackService.Invalid += WebpackInvalid;

            _heartbeatTimer = new Timer(EmitHeartbeat, null, 0, 1000);
        }

        public async Task Invoke(HttpContext context)
        {
            /* filter out our requests */
            if (context.Request.Method != "GET")
            {
                await _next(context);
                return;
            }

            if (context.Request.Path != "/__webpack_hmr")
            {
                await _next(context);
                return;
            }

            /* register for callback when request completes */
            context.Response.OnCompleted(OnResponseCompleted, context);

            /* set some headers */
            context.Response.Headers["Cache-Control"] = "no-cache, no-transform";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.ContentType = "text/event-stream;charset=utf-8";

            /* 
               This only returns when the client disconnects,
               so the connection is kept open.
            */
            await RegisterClient(context);
        }

        private Task OnResponseCompleted(object contextObj)
        {
            var context = contextObj as HttpContext;
            //UnregisterClient(context);
            return Task.FromResult<object>(null);
        }

        private List<HmrClient> _clients = new List<HmrClient>();

        private Task RegisterClient(HttpContext context)
        {
            var client = new HmrClient(context);
            _clients.Add(client);
            _logger.LogInformation("Client [{0}] connected", client.Description);
            return client.Wait.Task;
        }

        private void UnregisterClient(HttpContext context)
        {
            var client = _clients.SingleOrDefault(hc => hc.Context == context);

            if (client != null)
            {
                _logger.LogInformation("Client [{0}] disconnected", client.Description);

                _clients.Remove(client);
                client.Wait.SetResult(null);
            }
        }

        private void WebpackValid(object sender, JToken e)
        {
            _logger.LogInformation("Bundle is now valid {0}", "\u2705");

            var msg = e as JObject;
            msg["action"] = "built";
            Emit(msg.ToString(Newtonsoft.Json.Formatting.None));
        }

        private void WebpackInvalid(object sender, EventArgs e)
        {
            _logger.LogWarning("Bundle is now invalid {0}", "\u274C");

            Emit("{ \"action\": \"building\" }");
        }

        private static readonly string HEARTBEAT = "\uD83D\uDC93";
        private void EmitHeartbeat(object state)
        {
            _logger.LogDebug("Emitting heartbeat {0}", HEARTBEAT);
            Emit(HEARTBEAT);
        }

        private async void Emit(string payload)
        {
            payload = String.Format("data: {0}\r\n\r\n", payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(payload);

            var clients = new List<HmrClient>();
            clients.AddRange(_clients);

            clients.ForEach(async (client) =>
            {
                try
                {
                    await client.Context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                    await client.Context.Response.Body.FlushAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unexpected error", ex);
                    UnregisterClient(client.Context);
                }
            });
        }
    }

    internal class HmrClient
    {
        public HmrClient(HttpContext context)
        {
            Context = context;
            Wait = new TaskCompletionSource<object>();
            Description = GetClientAddress(context);
        }
        public TaskCompletionSource<object> Wait { get; private set; }
        public HttpContext Context { get; private set; }
        public string Description { get; }

        private string GetClientAddress(HttpContext context)
        {
            if (context == null)
                return "unknown";
            else if (context.Connection == null)
                return "unknown";
            else if (context.Connection.IsLocal)
                return "localhost";
            else if (context.Connection.RemoteIpAddress == null)
                return "unknown";
            else
                return context.Connection.RemoteIpAddress.ToString();
        }
    }
}