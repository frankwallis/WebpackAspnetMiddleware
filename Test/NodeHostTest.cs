using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Redouble.AspNet.Webpack.Test
{
    public class NodeHostTests
    {
        private ILogger GetLogger()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            return loggerFactory.CreateLogger("Test");
        }

        private CancellationToken GetApplicationStopping()
        {
            return new CancellationToken();
        }

        [Fact]
        public async Task NodeHost_CallsMethod()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback(null, 'result1'); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                var result = await host.Invoke<string>("method1", new object[0]);
                Assert.Equal("result1", result);
            }
        }

        [Fact]
        public async Task NodeHost_CallsMethodWithArgs()
        {
            var script = "module.exports = function() { return { method1: function(arg1, arg2, callback) { callback(null, arg1 + arg2); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                var result = await host.Invoke<string>("method1", new object[] { "quick", "fox" });
                Assert.Equal("quickfox", result.ToString());
            }
        }

        [Fact]
        public async Task NodeHost_HandlesMissingMethod()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback('an error occurred', null); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                try
                {
                    var result = await host.Invoke<object>("method2", new object[0]);
                    Assert.False(true);
                }
                catch (Exception ex)
                {
                    Assert.Equal("method [method2] does not exist", ex.Message);
                }
            }
        }

        [Fact]
        public async Task NodeHost_HandlesMethodError()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback(new Error('an error occurred'), null); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                try
                {
                    var result = await host.Invoke<object>("method1", new object[0]);
                    Assert.False(true);
                }
                catch (Exception ex)
                {
                    Assert.Equal("an error occurred", ex.Message);
                }
            }
        }

        [Fact]
        public async Task NodeHost_HandlesIncorrectArgs()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback(null, 42); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                try
                {
                    var result = await host.Invoke<int>("method1", new object[] { "stokes" });
                    Assert.False(true);
                }
                catch (Exception ex)
                {
                    Assert.Equal("incorrect number of arguments for method [method1]", ex.Message);
                }
            }
        }

        [Fact]
        public async Task NodeHost_HandlesUnicodeCharactersFromNode()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback(null, '[’]'); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                var result = await host.Invoke<string>("method1");
                Assert.Equal("[’]", result);
            }
        }

        [Fact]
        public async Task NodeHost_HandlesUnicodeCharactersToNode()
        {
            var script = "module.exports = function() { return { method1: function(str, callback) { callback(null, str === '[’]'); }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                var result = await host.Invoke<bool>("method1", "[’]");
                Assert.True(result);
            }
        }

        [Fact]
        public async Task NodeHost_ConvertsFromBas64StringToByteArray()
        {
            var script = "module.exports = function() { return { method1: function(str, callback) { callback(null, Buffer.from(str).toString('base64')) }}}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                var result = await host.Invoke<byte[]>("method1", "BinaryData");
                Assert.Equal(result, System.Text.Encoding.UTF8.GetBytes("BinaryData"));
            }
        }

        [Fact]
        public async Task NodeHost_RaisesEventsWithObjectArgs()
        {
            var script = "module.exports = function(emit) { return { start: function(callback) { emit('event1', { arg1: 'arg1' }); callback(null, 42); } }}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                string emitEvt = "";
                dynamic emitArgs = null;

                host.Emit += (sender, e) =>
                {
                    emitEvt = e.Name;
                    emitArgs = e.Args;
                };

                var result = await host.Invoke<Int64>("start", new object[0]);
                Assert.Equal((Int64)42, result);

                Assert.Equal("event1", emitEvt);
                Assert.Equal("arg1", emitArgs.arg1.ToString());
            }
        }

        [Fact]
        public async Task NodeHost_RaisesEventsWithStringArgs()
        {
            var script = "module.exports = function(emit) { return { start: function(callback) { emit('event1', 'arg1'); callback(null, 42); } }}";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), null))
            {
                string emitEvt = "";
                dynamic emitArgs = null;

                host.Emit += (sender, e) =>
                {
                    emitEvt = e.Name;
                    emitArgs = e.Args;
                };

                var result = await host.Invoke<Int64>("start", new object[0]);
                Assert.Equal((Int64)42, result);

                Assert.Equal("event1", emitEvt);
                Assert.Equal("arg1", emitArgs.ToString());
            }
        }

        [Fact]
        public async Task NodeHost_SetsEnvironmentVariables()
        {
            var script = "module.exports = function() { return { retrieve: function(callback) { callback(null, process.env.TESTVAR); }}}";

            var environmentVariables = new Dictionary<string, string>();
            environmentVariables["TESTVAR"] = "TESTVALUE";

            using (var host = await NodeHost.CreateFromScript(script, "", GetApplicationStopping(), GetLogger(), environmentVariables))
            {
                string emitEvt = "";
                dynamic emitArgs = null;

                host.Emit += (sender, e) =>
                {
                    emitEvt = e.Name;
                    emitArgs = e.Args;
                };

                var result = await host.Invoke<string>("retrieve", new object[0]);
                Assert.Equal("TESTVALUE", result);
            }
        }
    }
}
