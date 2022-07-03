# CSharpTools.Websocket
This library contains a helper class to make using the websocket-sharp library by sta easier.  

## Dependencies:
- [websocket-sharp](https://github.com/sta/websocket-sharp)

## Installation:
Grab the latest build from the releases folder [here](./bin/Release/netstandard2.0/CSharpTools.Websocket.dll).  
Once you have done that, add the dll to your projects refrence files.

## Examples:
### Client:
```cs
Uri uri = new Uri("ws://127.0.0.1:8080/");
WebsocketClientWrapper websocket;
if (!WebsocketClientManager.TryGetOrCreateConnection(uri, out websocket))
{
    Console.WriteLine("Failed to create websocket");
    return;
}
else Console.WriteLine("Created websocket");

websocket.onOpen += () => Console.WriteLine("Client onOpen");
websocket.onError += (error) => Console.WriteLine($"Client onError {error.Message}");
websocket.onClose += (data) => Console.WriteLine($"Client onClose {data.Reason}");
websocket.onMessage += (data) => Console.WriteLine($"Client onMessage {data.Data}");
websocket.onDispose += () => Console.WriteLine("Client onDispose");

Console.WriteLine("Press enter to exit...");
Console.ReadLine();

if (WebsocketClientManager.TryRemoveConnection(uri)) Console.WriteLine("Removed websocket");
else Console.WriteLine("Failed to remove websocket");
```

### Server:
```cs
Uri serverUri = new Uri("ws://0.0.0.0:8080/");
WebsocketServiceWrapper service;
if (!WebsocketServerManager.TryGetOrCreateService(serverUri, out service))
{
    Console.WriteLine("Failed to create service");
    return;
}
else Console.WriteLine("Created service");

service.onOpen += (id) => Console.WriteLine($"Server onOpen {id}");
service.onError += (id, error) => Console.WriteLine($"Server onError {id} {error.Message}");
service.onClose += (id, data) => Console.WriteLine($"Server onClose {id} {data.Reason}");
service.onMessage += (id, data) => Console.WriteLine($"Server onMessage {id} {data.Data}");
service.onDispose += () => Console.WriteLine("Server onDispose");

Console.WriteLine("Press enter to exit...");
Console.ReadLine();

if (WebsocketServerManager.TryRemoveService(serverUri)) Console.WriteLine("Removed service");
else Console.WriteLine("Failed to remove service");
```