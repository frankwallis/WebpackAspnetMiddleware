using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Redouble.AspNet.Webpack.Test
{
    public class DevServerTests
    {
        private const string DEFAULT_RESPONSE = "X-X default response X-X";

        private TestServer CreateServer(IWebpackService webpackService)
        {
            var mockServiceDescriptor = new ServiceDescriptor(typeof(IWebpackService), webpackService);

            var builder = new WebHostBuilder()
               .Configure(app =>
                       {
                           app.UseWebpackDevServer();
                           app.Run(async context =>
                           {
                               await context.Response.WriteAsync(DEFAULT_RESPONSE);
                           });
                       })
               .ConfigureLogging((hostingContext, factory) => factory.AddConsole())
               .ConfigureServices(services => services.Add(mockServiceDescriptor));

            return new TestServer(builder);
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

                Assert.Equal(2, response.Content.Headers.Count());
                Assert.Equal("js", response.Content.Headers.GetValues("Content-Type").FirstOrDefault());
                Assert.Equal("9", response.Content.Headers.GetValues("Content-Length").FirstOrDefault());
            }
        }

        [Fact]
        public async Task DevServer_SetsContentLengthInBytes()
        {
            // Arrange
            var mock = new WebpackServiceMock();
            mock.AddFile("/public/bundle.js", "[’]", "js"); // unicode

            using (var server = CreateServer(mock))
            {
                // Act
                // Actual request.
                var response = await server.CreateRequest("/public/bundle.js").SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal("[’]", await response.Content.ReadAsStringAsync());

                Assert.Equal(2, response.Content.Headers.Count());
                Assert.Equal("5", response.Content.Headers.GetValues("Content-Length").FirstOrDefault());
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

        [Fact]
        public async Task DevServer_Returns404WhenWebpackFileNotFound()
        {
            // Arrange
            var mock = new WebpackServiceMock();
            mock.AddNonExistantFile("/public/bundle.js");

            using (var server = CreateServer(mock))
            {
                // Act
                // Actual request.
                var response = await server.CreateRequest("/public/bundle.js").SendAsync("GET");

                // Assert
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }
    }
}
