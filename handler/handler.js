module.exports = function (emit) {

   var webpack = require('webpack');
   var MemoryFileSystem = require("memory-fs");
   var mime = require("mime");

   var instance = null;
   var valid = false;

   function onInvalid(compiler) {
      if (valid) emit('invalid');
      valid = false;
   }

   function onInvalidAsync(compiler, callback) {
      onInvalid(compiler);
      callback();
   }

   function onValid(stats) {
      valid = true;
      stats = stats.toJson();

      var map = stats.modules.reduce(function (result, module) {
         result[module.id] = module.name;
         return result;
      }, {});

      var args = {
         name: stats.name,
         time: stats.time,
         hash: stats.hash,
         warnings: stats.warnings || [],
         errors: stats.errors || [],
         modules: map
      };

      emit('valid', args);
      process.nextTick(flushPending);
   }

   function start(configPath, logLevelEnum, callback) {
      if (instance) return callback(new Error("webpack is already started"))

      var webpackConfig = require(configPath);
      var compiler = webpack(webpackConfig);
      var fs = new MemoryFileSystem();

      compiler.plugin("invalid", onInvalid);
      compiler.plugin("watch-run", onInvalidAsync);
      compiler.plugin("run", onInvalid);
      compiler.plugin("done", onValid);

      compiler.outputFileSystem = fs;

      var levels = ['none', 'errors-only', 'minimal', 'normal', 'verbose'];
      var logLevel = levels[logLevelEnum] || 'normal';

      instance = compiler.watch({}, function (err, stats) {
         var msg = stats.toString(logLevel);
         if (msg) console.log(msg);

         instance.fs = fs;
         callback(err);
      });
   }

   var pending = [];

   function flushPending() {
      if (valid) {
         pending.forEach(cb => cb());
         pending = [];
      }
   }

   function runOnValid(cb) {
      if (valid) cb();
      else pending.push(cb);
   }

   function getFile(filename, callback) {
      if (!instance) return callback(new Error("webpack is not started"));

      runOnValid(function () {
         var mimeType = mime.lookup(filename);

         instance.fs.readFile(filename, 'base64', function (err, contents) {
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
