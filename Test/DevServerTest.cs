using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Redouble.Aspnet;

namespace Redouble.Aspnet.Test
{
    public class DevServerTests
    {
       private const string DEFAULT_RESPONSE = "X-X default response X-X";
       
        private TestServer CreateServer(IWebpackService webpackService) 
        {
           var mockServiceDescriptor = new ServiceDescriptor(typeof(IWebpackService), webpackService);
           
           return TestServer.Create(app =>
            {
                app.UseMiddleware<WebpackDevServer>();
                app.Run(async context =>
                {
                    await context.Response.WriteAsync(DEFAULT_RESPONSE);
                });
            },
            services => services.Add(mockServiceDescriptor));           
        }
        
        [Fact]
        public async Task DevServer_SetsResponseHeaders()
        {
           // Arrange
           var mock = new WebpackServiceMock();
           mock.AddFile("/public/bundle.js", "bundle.js", "js"); 
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.
                var response = await server.CreateRequest("/public/bundle.js").SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal("bundle.js", await response.Content.ReadAsStringAsync());
                
                Assert.Equal(1, response.Headers.Count());
                Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
                
                Assert.Equal(2, response.Content.Headers.Count());                
                Assert.Equal("js", response.Content.Headers.GetValues("Content-Type").FirstOrDefault());
                Assert.Equal("9", response.Content.Headers.GetValues("Content-Length").FirstOrDefault());
            }
        }

        [Fact]
        public async Task DevServer_IgnoresNonWebpackFiles()
        {
           // Arrange
           var mock = new WebpackServiceMock();
           mock.AddFile("/public/bundle.js", "bundle.js", "js"); 
                                         
            using (var server = CreateServer(mock))
            {      
                // Act
                // Actual request.
                var response = await server.CreateRequest("/public/notbundle.js").SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(DEFAULT_RESPONSE, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
