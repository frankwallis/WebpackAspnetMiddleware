module.exports = function (emit) {

  var webpack = require('webpack');
  var MemoryFileSystem = require('memory-fs');
  var mime = require('mime');

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

  function onValid(statsOrMultiStats) {
    valid = true;
    statsOrMultiStats = statsOrMultiStats.toJson();
    var statsList = statsOrMultiStats.children.length
      ? statsOrMultiStats.children
      : [statsOrMultiStats];

    var argsList = statsList.map(stats => {
      var moduleMap = stats.modules.reduce(function (result, module) {
        result[module.id] = module.name;
        return result;
      }, {});

      return {
        name: stats.name,
        time: stats.time,
        hash: stats.hash,
        warnings: stats.warnings || [],
        errors: stats.errors || [],
        modules: moduleMap
      };
    });

    emit('valid', argsList);
    process.nextTick(flushPending);
  }

  function onLogStats(statsOrMultiStats, logLevel) {
    // Annoying but seems to be the only way to get colors when using a preset
    var presetToOptions = statsOrMultiStats.constructor.presetToOptions
      ? statsOrMultiStats.constructor.presetToOptions
      : statsOrMultiStats.stats[0].constructor.presetToOptions;

    var options = presetToOptions(logLevel);
    options.colors = true;

    var msg = statsOrMultiStats.toString(options);
    if (msg) emit('log', msg);
  }

  function start(configPath, startParams, callback) {
    if (instance) return callback(new Error('webpack is already started'))

    // back-compatibility with 2.2.2, can be removed after major version bump
    if (typeof startParams === 'number') {
      startParams = {
        logLevel: startParams,
        envParam: process.env.NODE_ENV
      };
    }

    var webpackConfig = require(configPath);
    if (typeof webpackConfig === 'function') {
      webpackConfig = webpackConfig(startParams.envParam);
    }

    var levels = ['none', 'errors-only', 'minimal', 'normal', 'verbose'];
    var logLevelName = levels[startParams.logLevel] || 'normal';

    var compiler = webpack(webpackConfig);
    var fs = new MemoryFileSystem();
    compiler.outputFileSystem = fs;

    if (compiler.hooks) {
      compiler.hooks.invalid.tap('webpack-aspnet-middleware', onInvalid);
      compiler.hooks.watchRun.tap('webpack-aspnet-middleware', onInvalid);
      compiler.hooks.run.tap('webpack-aspnet-middleware', onInvalid);
      compiler.hooks.done.tap('webpack-aspnet-middleware', function (stats) {
        onLogStats(stats, logLevelName);
        onValid(stats);
      });

      instance = compiler.watch({}, function (err, stats) {
        callback(err);
      });
      instance.fs = fs;
    } else {
      // back-compatibility for webpack 3
      compiler.plugin('invalid', onInvalid);
      compiler.plugin('watch-run', onInvalidAsync);
      compiler.plugin('run', onInvalid);
      compiler.plugin('done', function (stats) {
        onLogStats(stats, logLevelName);
        onValid(stats);
      });

      instance = compiler.watch({}, function (err, stats) {
        instance.fs = fs;
        callback(err);
      });
    }
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
    if (!instance) return callback(new Error('webpack is not started'));

    runOnValid(function () {
      var mimeType = mime.lookup(filename);

      instance.fs.readFile(filename, 'base64', function (err, contents) {
        callback(err, { contents: contents, mimeType: mimeType });
      });
    });
  }

  function stop(callback) {
    if (!instance) return callback(new Error('webpack is not started'));
    instance.close(callback);
  }

  return {
    start: start,
    stop: stop,
    getFile: getFile
  };
}
