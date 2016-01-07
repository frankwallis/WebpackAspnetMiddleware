using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNet.NodeServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Redouble.Aspnet.Webpack
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

        public NodeHost(string entryPointScript, string projectPath, string commandLineArguments = null) : base(entryPointScript, projectPath, commandLineArguments)
        {
        }

        public static NodeHost Create(string handlerFile, string projectPath)
        {
            var hostScript = EmbeddedResourceReader.Read(typeof(NodeHost), "/Content/node-host.js");
            return new NodeHost(hostScript, projectPath, "\"" + handlerFile + "\"");
        }

        public static NodeHost CreateFromScript(string handlerScript, string projectPath)
        {
            var handlerFile = new StringAsTempFile(handlerScript);
            var result = NodeHost.Create(handlerFile.FileName, projectPath);
            result._handlerFile = handlerFile;
            return result;
        }

        private async Task EnsureClient()
        {
            await this.EnsureReady();

            if (this._client == null)
            {
                this._client = new TcpClient();
                
                await this._client.ConnectAsync("127.0.0.1", _portNumber);   
                this._client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);             
                this._client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);             
                this._stream = this._client.GetStream();
                
                Task.Factory.StartNew(this.ReceiveAll);
            }
        }

        private async void ReceiveAll() {
            try
            {
               while(true) 
               {
                  await this.Receive();
               }
            }
            catch (IOException ex)
            {
                System.Console.WriteLine("error");
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
            this.OnEmit(method, args as JToken);
        }

        private void HandleResponse(int id, object args)
        {
            var pending = this._pendingTasks.Find((task) => task.Id == id);
            this._pendingTasks.Remove(pending);
            pending.Deferred.SetResult(args as JToken);
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
            await this.EnsureClient();

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
            var buffer = System.Text.Encoding.UTF8.GetBytes(msgStr);
            await this._stream.WriteAsync(buffer, 0, buffer.Length);
            await this._stream.FlushAsync();

            return await pending.Deferred.Task;
        }

        public new async Task<T> Invoke<T>(string methodName, params object[] args)
        {
            var jtoken = await Invoke(methodName, args);
            return jtoken.ToObject<T>();
        }

        public async override Task<T> Invoke<T>(NodeInvocationInfo invocationInfo)
        {
            throw new NotImplementedException("Use overloaded Invoke method instead");
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