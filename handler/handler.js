module.exports = function(emit) {
   
   var webpack = require('webpack');
   var MemoryFileSystem = require("memory-fs");
   var mime = require("mime");
   
   var instance = null;
   var valid = false;

   function onInvalid(compiler, callback) {
      if (valid) emit('invalid');
      valid = false;
      if (callback) callback();
   }

   function onValid(stats) {
      valid = true;
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
      flushPending();
   }
   
   function start(configPath, opts, callback) {
      if (instance) return callback(new Error("webpack is already started"))
      
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

   var pending = [];
   
   function flushPending() {
      pending.forEach(cb => cb);
      pending = [];
   }
   
   function runOnValid(cb) {
      if (valid) cb();
      else pending.push(cb);
   }
   
   function getFile(filename, callback) {
      if (!instance) return callback(new Error("webpack is not started"));

      runOnValid(function() {
         var mimeType = mime.lookup(filename);
         
         instance.fs.readFile(filename, 'utf8', function(err, contents) {
            callback(err, { contents: contents, mimeType: mimeType });         
         });
      });
   }
   
   function stop(callback) {
      if (!instance) return callback(new Error("webpack is not started"));
      instance.close(callback);
   }
   
   return {
      start: start,
      stop: stop,
      getFile: getFile
   };
}
