using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Redouble.AspNet.Webpack
{
    public class WebpackHotReload
    {
        private RequestDelegate _next;
        private IWebpackService _webpackService;
        private Timer heartbeatTimer;

        public WebpackHotReload(RequestDelegate next, IWebpackService webpackService)
        {
            _next = next;
            _webpackService = webpackService;

            _webpackService.Valid += WebpackValid;
            _webpackService.Invalid += WebpackInvalid;

            heartbeatTimer = new Timer(EmitHeartbeat, null, 0, 10000);
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

            context.Response.OnCompleted(OnResponseCompleted, context.Request);

            /* set some headers */
            context.Response.Headers["Cache-Control"] = "no-cache, no-transform";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.ContentType = "text/event-stream;charset=utf-8";

            await RegisterClient(context);
        }

        private Task OnResponseCompleted(object request)
        {
            System.Console.WriteLine("HMR Disconnected");
            //UnregisterClient(request as HttpRequest);
            return Task.FromResult<object>(null);
        }

        private List<HmrClient> _clients = new List<HmrClient>();

        private Task RegisterClient(HttpContext context)
        {
            var client = new HmrClient(context);
            _clients.Add(client);
            return client.Wait.Task;
        }
        private void UnregisterClient(HmrClient client)
        {
            _clients.Remove(client);
            client.Wait.SetResult(null);
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

        private void EmitHeartbeat(object state)
        {
            Emit("\uD83D\uDC93");
        }

        private async void Emit(string payload)
        {
            payload = String.Format("data: {0}\r\n\r\n", payload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(payload);

            var clients = new List<HmrClient>();
            clients.AddRange(_clients);
            //Console.WriteLine(payload);
            clients.ForEach(async (client) =>
            {
                try
                {
                    await client.Context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                    await client.Context.Response.Body.FlushAsync();
                }
                catch (Exception ex)
                {
                   UnregisterClient(client);
                   Console.WriteLine(ex);                    
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
        }
        public TaskCompletionSource<object> Wait { get; private set; }
        public HttpContext Context { get; private set; }
    }
}