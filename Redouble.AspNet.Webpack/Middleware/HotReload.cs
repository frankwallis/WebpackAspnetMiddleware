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
    public class HotReload
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private ILogger _logger;
        private Timer _heartbeatTimer;

        public HotReload(RequestDelegate next,
            IWebpackService webpackService,
            ILogger<HotReload> logger)
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

            /* set some headers to force the response to auto-chunk and keep-alive */
            context.Response.Headers["Cache-Control"] = "no-cache, no-transform";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.ContentType = "text/event-stream;charset=utf-8";

            /* register the client to receive events */
            var client = RegisterClient(context);

            /* 
               This only completes when the client disconnects,
               so the connection is kept open.
            */
            await client.CompletionSource.Task;
            
            /*
               This prevents HttpFrame.WriteChunkedResponseSuffix from throwing
               an EPIPE error with stacktrace due to the socket being disconnected
            */
            throw new QuietException("Redouble.AspNet.Webpack.HotReload: client disconnected");
        }

        private List<HmrClient> _clients = new List<HmrClient>();

        private HmrClient RegisterClient(HttpContext context)
        {
            var client = new HmrClient(context);
            _clients.Add(client);
            _logger.LogInformation("Client [{0}] connected", client.Description);
            return client;
        }

        private void UnregisterClient(HttpContext context)
        {
            var client = _clients.SingleOrDefault(hc => hc.Context == context);
            _clients.Remove(client);
            _logger.LogInformation("Client [{0}] disconnected", client.Description);
            client.CompletionSource.SetResult(null);
        }

        private void WebpackValid(object sender, JToken e)
        {
            var msg = e as JObject;
            msg["action"] = "built";
            Emit(msg.ToString(Newtonsoft.Json.Formatting.None));
        }

        private void WebpackInvalid(object sender, EventArgs e)
        {
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
                catch (Exception ex) // change to IOException in rc2
                {
                    _logger.LogDebug("Write failed: ", ex);
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
            CompletionSource = new TaskCompletionSource<object>();
            Description = GetClientAddress(context);
        }
        public TaskCompletionSource<object> CompletionSource { get; private set; }
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
    
    class QuietException : Exception {
       public QuietException(string message): base(message) {}
       
       public override string ToString() {
          return Message;
       }
    }
}