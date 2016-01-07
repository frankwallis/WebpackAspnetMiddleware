module.exports = function(emit) {
   
   var webpack = require('webpack');
   var MemoryFileSystem = require("memory-fs");
   var mime = require("mime");
   
   var instance = null;

   function onInvalid(compiler, callback) {
      emit('invalid');
      if (callback) callback();
   }

   function onValid(stats) {
      stats = stats.toJson();

      var map = stats.modules.reduce(function(result, module) {
         result[module.id] = module.name;
         return result;   
      }, {});
      
      var args = {
         time: stats.time,
         hash: stats.hash,
         warnings: stats.warnings || [],
         errors: stats.errors || [],
         modules: map
      };
      
      emit('valid', args);
   }
   
   function start(configPath, opts, callback) {
      if (instance) callback(new Error("webpack is already started"))
      
      var webpackConfig = require(configPath);
      var compiler = webpack(webpackConfig);
      var fs = new MemoryFileSystem();

      compiler.plugin("invalid", onInvalid);
      compiler.plugin("watch-run", onInvalid);
      compiler.plugin("run", onInvalid);
      compiler.plugin("done", onValid);

      compiler.outputFileSystem = fs;

      instance = compiler.watch({}, function(err) {
         instance.fs = fs;
         callback(err);        
      });  
   }

   function getFile(filename, callback) {
      if (!instance) callback(new Error("webpack is not started"));
      
      var mimeType = mime.lookup(filename);
      instance.fs.readFile(filename, 'utf8', function(err, contents) {
         callback(err, { contents: contents, mimeType: mimeType });         
      });      
   }
   
   function stop(callback) {
      if (!instance) callback(new Error("webpack is not started"));
      instance.close(callback);
   }
   
   return {
      start: start,
      stop: stop,
      getFile: getFile
   };
}
