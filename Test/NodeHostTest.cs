using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Redouble.AspNet.Webpack;

namespace Redouble.AspNet.Webpack.Test
{
    public class NodeHostTests
    {
        [Fact]
        public async Task NodeHost_CallsMethod()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback(null, 'result1'); }}}";

            using (var host = NodeHost.CreateFromScript(script, ""))
            {
                var result = await host.Invoke<string>("method1", new object[0]);
                Assert.Equal("result1", result);
            }
        }

        [Fact]
        public async Task NodeHost_CallsMethodWithArgs()
        {
            var script = "module.exports = function() { return { method1: function(arg1, arg2, callback) { callback(null, arg1 + arg2); }}}";

            using (var host = NodeHost.CreateFromScript(script, ""))
            {
                var result = await host.Invoke<string>("method1", new object[] { "quick", "fox" });
                Assert.Equal("quickfox", result.ToString());
            }
        }

        [Fact]
        public async Task NodeHost_HandlesMissingMethod()
        {
            var script = "module.exports = function() { return { method1: function(callback) { callback('an error occurred', null); }}}";

            using (var host = NodeHost.CreateFromScript(script, ""))
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

            using (var host = NodeHost.CreateFromScript(script, ""))
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

            using (var host = NodeHost.CreateFromScript(script, ""))
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
        public async Task NodeHost_RaisesEvents()
        {
            var script = "module.exports = function(emit) { return { start: function(callback) { emit('event1', { arg1: 'arg1' }); callback(null, 42); } }}";

            using (var host = NodeHost.CreateFromScript(script, ""))
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
                //Assert.Equal("args1", emitArgs.arg1.toString());
            }
        }

    }
}
