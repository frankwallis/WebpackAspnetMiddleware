using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Redouble.AspNet.Webpack.Test
{
    public class HotReloadTests
    {
        private const string DEFAULT_RESPONSE = "X-X default response X-X";

        private TestServer CreateServer(IWebpackService mockWebpackService)
        {
            var builder = new WebHostBuilder()
               .Configure(app =>
                {
                    app.UseWebpackHotReload();
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync(DEFAULT_RESPONSE);
                    });
                })
               .ConfigureLogging((hostingContext, factory) => factory.AddConsole())
               .ConfigureServices(services =>
               {
                   services.Add(new ServiceDescriptor(typeof(IWebpackService), mockWebpackService));
               });

            return new TestServer(builder);
        }
/*
        [Fact]        
        public async Task DevServer_SetsChunkedEncoding()
        {
            // Arrange
            var mock = new WebpackServiceMock();

            using (var server = CreateServer(mock))
            {      
                using (var client = server.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/event-stream");
                    var response = await client.GetAsync("/__webpack_hmr");

                    // Assert
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(2, response.Headers.Count());
                    Assert.True(response.Headers.CacheControl.NoTransform);
                    Assert.True(response.Headers.CacheControl.NoCache);
                    Assert.True(response.Headers.Connection.Contains("keep-alive"));
                    Assert.Null(response.Headers.TransferEncodingChunked);
                    Assert.Null(response.Headers.ConnectionClose);                
                    Assert.Null(response.Content.Headers.ContentLength);
                }         
            }
        }
*/
        [Fact]
        public async Task HotReload_EmitsHeartbeats()
        {
            var mock = new WebpackServiceMock();

            using (var server = CreateServer(mock))
            {
                using (var client = server.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/event-stream");
                    var stream = await client.GetStreamAsync("/__webpack_hmr");

                    var buffer = new byte[256];
                    var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(14, byteCount1);

                    var cancellationTokenSource = new CancellationTokenSource(10000);
                    var byteCount2 = await stream.ReadAsync(buffer, 0, 256, cancellationTokenSource.Token);

                    Assert.Equal(14, byteCount2);
                    Assert.Equal("data: \uD83D\uDC93\r\n\r\n", System.Text.Encoding.UTF8.GetString(buffer).Substring(0, byteCount2 - 2));
                }
            }
        }

        [Fact]
        public async Task HotReload_EmitsOnValid()
        {
            var mock = new WebpackServiceMock();
            mock.AddFile("/public/bundle.js", "bundle.js", "js");

            using (var server = CreateServer(mock))
            {
                using (var client = server.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/event-stream");                    
                    var stream = await client.GetStreamAsync("/__webpack_hmr");

                    var buffer = new byte[256];
                    var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(14, byteCount1);

                    var e = new JArray();
                    e.Add(new JObject());
                    mock.OnValid(e);

                    // TODO
                    var byteCount2 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(28, byteCount2);
                    Assert.Equal("data: {\"action\":\"built\"}\r\n\r\n", System.Text.Encoding.UTF8.GetString(buffer).Substring(0, byteCount2));
                }
            }
        }

        [Fact]
        public async Task HotReload_DisconnectsOnShutdown()
        {
            var mockService = new WebpackServiceMock();
            mockService.AddFile("/public/bundle.js", "bundle.js", "js");

            using (var server = CreateServer(mockService))
            {
                using (var client = server.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/event-stream");                    
                    var stream = await client.GetStreamAsync("/__webpack_hmr");

                    var buffer = new byte[256];
                    var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(14, byteCount1);

                    await server.Host.StopAsync();

                    // For some reason this doesn't throw, but it does return 0 bytes
                    var byteCount2 = await stream.ReadAsync(buffer, 0, 256);
                    Assert.Equal(0, byteCount2);
                }
            }
        }
    }
}
