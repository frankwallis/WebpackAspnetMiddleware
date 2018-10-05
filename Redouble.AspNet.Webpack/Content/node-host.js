var net = require('net');
var path = require('path');

var handlerScript = process.argv[process.argv.length - 1];
var handler = null;

var tcpServer = net.createServer(function (socket) {
  socket.setKeepAlive(true);
  socket.setNoDelay();

  try {
    handler = createHandler(socket);
  }
  catch (err) {
    console.error(err.message);
    tcpServer.close();
    return process.exit(1);
  }

  socket.on('error', function () { handleError(socket); });
  socket.on('data', function (buffer) {
    while (buffer.length > 0) {
      var size = buffer.readIntLE(0, 4);
      if (size <= 0) throw new Error("Invalid message");
      var msg = buffer.slice(4, size + 4);
      buffer = buffer.slice(size + 4);
      handleData(socket, msg);
    }
  });
});

tcpServer.on('error', (e) => {
  console.error('Error opening socket: [' + e.code + ']');
  tcpServer.close();
  return process.exit(1);
});

tcpServer.listen(0, '127.0.0.1', function () {
  // Send the port number to the NodeHost
  console.log('[Redouble.AspNet.Webpack.NodeHost:Listening on port ' + tcpServer.address().port + '\]');
  // Signal to the NodeServices base class that we're ready to accept invocations
  console.log('[Microsoft.AspNetCore.NodeServices:Listening]');
});

function handleError(socket) {
  socket.end();
}

function handleData(socket, data) {
  var msg = JSON.parse(data);
  var method = handler[msg.method];

  if (!method) {
    writeError(socket, msg.id, "method [" + msg.method + "] does not exist");
  }
  else if (method.length != msg.args.length + 1) {
    writeError(socket, msg.id, "incorrect number of arguments for method [" + msg.method + "]");
  }
  else {
    var callback = function (err, res) {
      if (err) writeError(socket, msg.id, err);
      else writeResponse(socket, msg.id, res);
    };

    var args = msg.args.concat(function (err, res) {
      // only callback once
      if (callback) callback(err, res);
      callback = null;
    });

    method.apply(handler, args);
  }
}

function writeResponse(socket, id, data) {
  var msg = {
    id: id,
    type: 'response',
    args: data
  }
  writeMessage(socket, msg);
}

function writeError(socket, id, err) {
  var msg = {
    id: id,
    type: 'error',
    args: err.message ? err.message : err.toString()
  }
  writeMessage(socket, msg);
}

function writeEvent(socket, event, data) {
  var msg = {
    type: 'event',
    method: event,
    args: data
  };
  writeMessage(socket, msg);
}

function writeMessage(socket, msg) {
  var msgStr = JSON.stringify(msg);
  var msgLength = Buffer.byteLength(msgStr);
  var buffer = Buffer.alloc(msgLength + 4);
  buffer.writeUInt32LE(msgLength << 0, 0);
  buffer.write(msgStr, 4);
  socket.write(buffer);
}

function createHandler(socket) {
  // load the handler module 
  var exportFunc = require(handlerScript);

  if (typeof exportFunc !== 'function')
    throw new Error("handler script should export a default function");

  // call the default export function   
  var handler = exportFunc(function (event, data) {
    writeEvent(socket, event, data);
  });

  return handler;
}
