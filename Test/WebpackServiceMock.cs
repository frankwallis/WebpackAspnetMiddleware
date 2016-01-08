using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;
using Microsoft.Extensions.PlatformAbstractions;
using Redouble.AspNet.Webpack;

namespace Redouble.AspNet.Webpack.Test
{
    public class WebpackServiceMock : IWebpackService
    {        
        public WebpackServiceMock()
        {
           _files = new Dictionary<string, IWebpackFile>();
        }
        
        private  Dictionary<string, IWebpackFile> _files;

        public IWebpackFile AddFile(string filename, string contents, string mimeType) {
           var result = new WebpackFile(contents, mimeType);
            _files.Add(filename, result);
            return result;
        }  
              
        public Task<IWebpackFile> GetFile(string filename) {
           return Task.FromResult(_files[filename]);
        }
        
        public void OnValid(ValidEventArgs e) {
           Valid(this, e);
        }
        public void OnInvalid() {
           Invalid(this, EventArgs.Empty);
        }

        public event EventHandler<ValidEventArgs> Valid;
        public event EventHandler Invalid;
        public bool IsWebpackFile(string filename) {
            return _files.ContainsKey(filename);   
        }
   }
}