using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;
using Redouble.AspNet.Webpack;

namespace Redouble.AspNet.Webpack.Test
{
    public class HotReloadTests
    {
       private const string DEFAULT_RESPONSE = "X-X default response X-X";
       
        private TestServer CreateServer(IWebpackService webpackService) 
        {
           var mockServiceDescriptor = new ServiceDescriptor(typeof(IWebpackService), webpackService);
           
           return TestServer.Create(app =>
            {
                app.UseMiddleware<WebpackHotReload>();
                app.Run(async context =>
                {
                    await context.Response.WriteAsync(DEFAULT_RESPONSE);
                });
            },
            services => services.Add(mockServiceDescriptor));           
        }
        
        [Fact]
        public async Task DevServer_SetsChunkedEncoding()
        {
           // Arrange
           var mock = new WebpackServiceMock();
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.
                //var response = await server.CreateRequest("/__webpack_hmr").SendAsync("GET");
                var client = server.CreateClient();
                var response = await client.GetAsync("/__webpack_hmr");
                                
                                System.Console.WriteLine(response.Content);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(3, response.Headers.Count());
                Assert.True(response.Headers.TransferEncodingChunked);
                Assert.Null(response.Headers.ConnectionClose);
                //Assert.Null(response.Content.Headers.ContentLength);
            }
        }
        
        [Fact]
        public async Task DevServer_KeepsConnectionAlive()
        {
           // Arrange
           var mock = new WebpackServiceMock();
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.
                var client = server.CreateClient();
                var stream = await client.GetStreamAsync("/__webpack_hmr");
                
                var buffer = new byte[256];
                var streamReader = new System.IO.StreamReader(stream);
                
                var handshake = await streamReader.ReadLineAsync();
                Assert.Equal(0, handshake.Length);


                var heartbeat = await streamReader.ReadLineAsync();
                Assert.Equal(0, heartbeat.Length);
                //var byteCount = await stream.ReadAsync(buffer, 0, 256);
                
                // Assert
                //Assert.Equal(1, byteCount);
                //Assert.Equal(System.Convert.ToByte('\n'), buffer[0]);
            }
        }

        [Fact]
        public async Task DevServer_EmitsHeartbeats()
        {
           // Arrange
           var mock = new WebpackServiceMock();
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.                
                var client = server.CreateClient();
                var stream = await client.GetStreamAsync("/__webpack_hmr");
                
                var buffer = new byte[256];
                var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                Assert.Equal(1, byteCount1);

                var cancellationTokenSource = new CancellationTokenSource(20000);                
                var byteCount2 = await stream.ReadAsync(buffer, 0, 256, cancellationTokenSource.Token);
                
                Assert.Equal(2, byteCount2);
                Assert.Equal(System.Convert.ToByte('\n'), buffer[0]);
            }
        }

        public async Task DevServer_EmitsOnValid()
        {
           // Arrange
           var mock = new WebpackServiceMock();
           mock.AddFile("/public/bundle.js", "bundle.js", "js"); 
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.
                var client = server.CreateClient();
                var stream = await client.GetStreamAsync("/__webpack_hmr");
                
                var buffer = new byte[256];
                var byteCount1 = await stream.ReadAsync(buffer, 0, 256);
                Assert.Equal(1, byteCount1);
                
                var e = new JObject();
                mock.OnValid(e);
                
                // TODO
                var byteCount2 = await stream.ReadAsync(buffer, 0, 256);
                Assert.Equal(1, byteCount2);
                Assert.Equal(System.Convert.ToByte('\n'), buffer[0]);
            }
        }
    }
}
