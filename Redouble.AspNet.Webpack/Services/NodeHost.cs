using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.NodeServices.HostingModels;

namespace Redouble.AspNet.Webpack
{
    public class EmitEventArgs : EventArgs
    {
        public string Name { get; set; }
        public JToken Args { get; set; }
    }

    public class NodeHost : OutOfProcessNodeInstance
    {
        private IDisposable _handlerFile;
        private TcpClient _client;
        private Stream _stream;

        public NodeHost(
            string entryPointScript, 
            string projectPath,
            string commandLineArguments,
            CancellationToken applicationStopping,
            ILogger logger,
            IDictionary<string, string> environmentVars
        ) : base (
            entryPointScript,
            projectPath,
            null,
            commandLineArguments,
            applicationStopping,
            logger,
            environmentVars,
            5000,
            false,
            0
        ) {
        }

        public static async Task<NodeHost> Create(string handlerFile, string projectPath, CancellationToken applicationStopping, ILogger logger, IDictionary<string, string> environmentVars)
        {
            var hostScript = EmbeddedResourceReader.Read(typeof(NodeHost), "/Content/node-host.js");
            var result = new NodeHost(hostScript, projectPath, "\"" + handlerFile + "\"", applicationStopping, logger, environmentVars);
            await result.Start();
            return result;
        }

        public static async Task<NodeHost> CreateFromScript(string handlerScript, string projectPath, CancellationToken applicationStopping, ILogger logger, IDictionary<string, string> environmentVars)
        {
            var handlerFile = new StringAsTempFile(handlerScript, applicationStopping);
            var result = await NodeHost.Create(handlerFile.FileName, projectPath, applicationStopping, logger, environmentVars);
            result._handlerFile = handlerFile;
            return result;
        }

        public Task Stopped { get; private set; }

        private async Task Start()
        {
            await InvokeExportAsync<Object>(CancellationToken.None, "", "");

            this._client = new TcpClient();
            await this._client.ConnectAsync("127.0.0.1", _portNumber);
            this._client.NoDelay = true;
            //this._client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            this._stream = this._client.GetStream();
            this.Stopped = this.ReceiveAll();
        }

        private async Task ReceiveAll()
        {
            try
            {
                while (this._client.Connected)
                {
                    await this.Receive();
                }
            }
            catch (Exception ex)
            {
                var pendingTasks = _pendingTasks.ToArray();
                _pendingTasks.Clear();
                foreach (var pending in pendingTasks)
                {
                    pending.Deferred.SetException(ex);
                }

                this.OnDisconnected();
            }
        }

        public async Task Receive()
        {
            byte[] header = new byte[4];

            if (await this._stream.ReadAsync(header, 0, header.Length) != 4)
                throw new Exception("Error reading header");

            int bufferLength = BitConverter.ToInt32(header, 0);
            var buffer = new byte[bufferLength];
            int bytesRead = 0;

            while (bytesRead < bufferLength)
            {
                var chunkSize = await this._stream.ReadAsync(buffer, bytesRead, bufferLength - bytesRead);
                bytesRead += chunkSize;
            }

            this.HandleDataReceived(buffer);
        }

        private void HandleDataReceived(byte[] buffer)
        {
            var msg = JsonConvert.DeserializeObject<NodeHostMessage>(System.Text.Encoding.UTF8.GetString(buffer), jsonSerializerSettings);

            if (msg.Type == "event")
            {
                this.HandleEvent(msg.Method, msg.Args);
            }
            else if (msg.Type == "response")
            {
                this.HandleResponse(msg.Id, msg.Args);
            }
            else if (msg.Type == "error")
            {
                this.HandleError(msg.Id, msg.Args);
            }
        }

        private void HandleEvent(string method, object args)
        {
            if (args is JToken)
                this.OnEmit(method, args as JToken);
            else
                this.OnEmit(method, new JValue(args));            
        }

        private void HandleResponse(int id, object args)
        {
            var pending = this._pendingTasks.Find((task) => task.Id == id);
            this._pendingTasks.Remove(pending);

            if (args is JToken)
                pending.Deferred.SetResult(args as JToken);
            else
                pending.Deferred.SetResult(new JValue(args));
        }

        private void HandleError(int id, object args)
        {
            var pending = this._pendingTasks.Find((task) => task.Id == id);
            this._pendingTasks.Remove(pending);
            pending.Deferred.SetException(new Exception(args.ToString()));
        }

        private List<NodeHostTask> _pendingTasks = new List<NodeHostTask>();
        private int _maxId = 0;

        public async Task<JToken> Invoke(string methodName, params object[] args)
        {
            var msg = new NodeHostMessage();
            msg.Type = "invoke";
            msg.Id = ++this._maxId;
            msg.Method = methodName;
            msg.Args = args;

            var pending = new NodeHostTask();
            pending.Id = msg.Id;
            pending.Deferred = new TaskCompletionSource<JToken>();
            this._pendingTasks.Add(pending);

            var msgStr = JsonConvert.SerializeObject(msg, jsonSerializerSettings);
            var msgLength = System.Text.Encoding.UTF8.GetByteCount(msgStr);
            var buffer = new byte[msgLength + 4];
            var header = BitConverter.GetBytes(msgLength);
            header.CopyTo(buffer, 0);
            System.Text.Encoding.UTF8.GetBytes(msgStr, 0, msgStr.Length, buffer, 4);

            await this._stream.WriteAsync(buffer, 0, buffer.Length);
            await this._stream.FlushAsync();

            return await pending.Deferred.Task;
        }

        public async Task<T> Invoke<T>(string methodName, params object[] args)
        {
            var jtoken = await Invoke(methodName, args);
            return jtoken.ToObject<T>();
        }

        protected override Task<T> InvokeExportAsync<T>(
            NodeInvocationInfo invocationInfo,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<T>(default(T));
        }

        public event EventHandler<EmitEventArgs> Emit;

        private void OnEmit(string eventName, JToken args)
        {
            if (this.Emit != null)
            {
                var e = new EmitEventArgs();
                e.Name = eventName;
                e.Args = args;
                this.Emit(this, e);
            }
        }

        private void OnDisconnected()
        {
            if (this.Disconnected != null)
                this.Disconnected(this, EventArgs.Empty);
        }
        public event EventHandler Disconnected;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (this._handlerFile != null)
                {
                    this._handlerFile.Dispose();
                    this._handlerFile = null;
                }

                if (this._client != null)
                {
                    (this._client as IDisposable).Dispose();
                    this._client = null;
                }

                if (this._stream != null)
                {
                    this._stream.Dispose();
                    this._stream = null;
                }
            }
        }

        /* 
            OutOfProcessNodeInstance attaches to stdin and stdout and the node process uses stdout to
            signal that it is up and running. 
            node-host.js writes a specially formatted message to stdout to tell us what port it is serving on.
        */
        private int _portNumber = 0;
        private readonly static Regex PortMessageRegex = new Regex(@"^\[Redouble.AspNet.Webpack.NodeHost:Listening on port (\d+)\]$");

        protected override void OnOutputDataReceived(string outputData)
        {
            var match = _portNumber != 0 ? null : PortMessageRegex.Match(outputData);
            if (match != null && match.Success)
            {
                this._portNumber = int.Parse(match.Groups[1].Captures[0].Value);
            }
            else
            {
                base.OnOutputDataReceived(outputData);
            }
        }

        /* JSON.NET settings */
        private readonly static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    internal class NodeHostMessage
    {
        public string Type { get; set; }
        public int Id { get; set; }
        public string Method { get; set; }
        public object Args { get; set; }
    }

    internal class NodeHostTask
    {
        public TaskCompletionSource<JToken> Deferred { get; set; }
        public int Id { get; set; }
    }
}
