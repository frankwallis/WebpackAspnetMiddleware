using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;

namespace Redouble.AspNet.Webpack
{
    public class HotReload
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private ILogger _logger;
        private Timer _heartbeatTimer;
        private IApplicationLifetime _lifetime;

        public HotReload(RequestDelegate next,
            IWebpackService webpackService,
            IApplicationLifetime lifetime,
            ILogger<HotReload> logger)
        {
            _next = next;
            _logger = logger;
            _lifetime = lifetime;

            _webpackService = webpackService;
            _webpackService.Valid += WebpackValid;
            _webpackService.Invalid += WebpackInvalid;

            _heartbeatTimer = new Timer(EmitHeartbeat, null, 0, webpackService.Options.Heartbeat);
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


            if (context.Request.Headers["Accept"] != "text/event-stream")
            {
                await _next(context);
                return;
            }

            /* set some headers to force the response to auto-chunk and keep-alive */
            context.Response.Headers["Cache-Control"] = "no-cache, no-transform";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.ContentType = "text/event-stream;charset=utf-8";
            context.Response.Body.Flush();

            /* register the client to receive events */
            var client = RegisterClient(context);

            try {
                /* This will complete when the client disconnects, or the application is being shutdown */
                await Task.WhenAny(client.ClientDisconnected.Task, _lifetime.ApplicationStopping.CompletionSource().Task);
            }
            finally {
                /* session has ended */
                UnregisterClient(client);
            }
        }

        private List<HmrClient> _clients = new List<HmrClient>();

        private HmrClient RegisterClient(HttpContext context)
        {
            var client = new HmrClient(context);
            _clients.Add(client);
            _logger.LogInformation("Client [{0}] connected", client.Description);
            return client;
        }

        private void UnregisterClient(HmrClient client)
        {
            _clients.Remove(client);
            _logger.LogInformation("Client [{0}] disconnected", client.Description);
        }

        private void WebpackValid(object sender, JToken e)
        {
            var msgList = e as JArray;
            foreach (var msg in msgList)
            {
                msg["action"] = "built";
                Emit(msg.ToString(Newtonsoft.Json.Formatting.None));
            }
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

        private void Emit(string payload)
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
                catch (IOException ex)
                {
                    _logger.LogDebug("Write failed: ", ex);
                    client.ClientDisconnected.TrySetResult(null);
                }
            });
        }
    }

    internal class HmrClient
    {
        public HmrClient(HttpContext context)
        {
            Context = context;
            Description = GetClientAddress(context);
            ClientDisconnected = context.RequestAborted.CompletionSource();
        }
        public TaskCompletionSource<object> ClientDisconnected { get; private set; }
        public HttpContext Context { get; private set; }
        public string Description { get; }

        private string GetClientAddress(HttpContext context)
        {
            if (context == null)
                return "unknown";
            else if (context.Connection == null)
                return "unknown";
            // else if (context.Connection.IsLocal)
            //    return "localhost";
            else if (context.Connection.RemoteIpAddress == null)
                return "unknown";
            else
                return context.Connection.RemoteIpAddress.ToString();
        }
    }

    internal static class ExtensionMethods {
        internal static TaskCompletionSource<object> CompletionSource(this CancellationToken tokenSource) {
            var taskCompletionSource = new TaskCompletionSource<object>();
            tokenSource.Register(completionSource => ((TaskCompletionSource<object>) completionSource).TrySetResult(null), taskCompletionSource);
            return taskCompletionSource;
        }
    }
}