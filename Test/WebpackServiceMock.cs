using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Linq;
using Redouble.AspNet.Webpack;

namespace Redouble.AspNet.Webpack.Test
{
    public class WebpackServiceMock : IWebpackService
    {
        public WebpackServiceMock()
        {
            _files = new Dictionary<string, IWebpackFile>();
        }

        private Dictionary<string, IWebpackFile> _files;

        public IWebpackFile AddFile(string filename, string contents, string mimeType)
        {
            var result = new WebpackFile(contents, mimeType);
            _files.Add(filename, result);
            return result;
        }

        public void AddNonExistantFile(string filename)
        {
            _files.Add(filename, null);
        }

        public Task<IWebpackFile> GetFile(string filename)
        {
            if (_files[filename] == null)
                return Task.FromException(new Exception("File not found")) as Task<IWebpackFile>;
            else
                return Task.FromResult(_files[filename]);
        }

        public void OnValid(JToken e)
        {
            Valid(this, e);
        }
        public void OnInvalid()
        {
            Invalid(this, EventArgs.Empty);
        }

        public event EventHandler<JToken> Valid;
        public event EventHandler Invalid;
        public bool IsWebpackFile(string filename)
        {
            return _files.ContainsKey(filename);
        }
    }
}