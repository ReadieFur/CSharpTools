# CSharpTools.Websocket
This library contains a helper class to make using the websocket-sharp library by sta easier.  

## Dependencies:
- [websocket-sharp](https://github.com/sta/websocket-sharp)

## Installation:
Grab the latest build from the releases folder [here](./bin/Release/CSharpTools.Websocket.dll).  
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

Console.WriteLine($"Websocket is connected: {websocket.isConnected}");
websocket.onOpen += () => Console.WriteLine("Client connection opened.");
websocket.onError += (error) => Console.WriteLine(error.Message);
websocket.onClose += (data) => Console.WriteLine(data.Reason);
websocket.onMessage += (data) => Console.WriteLine(data.Data);

Console.WriteLine("Press enter to exit...");
Console.ReadLine();

if (WebsocketClientManager.TryRemoveConnection(uri)) Console.WriteLine("Removed websocket");
else Console.WriteLine("Failed to remove websocket");
```

### Server:
```cs
Uri serverUri = new Uri("ws://0.0.0.0:8080/");
WebsocketServiceHelper service;
if (!WebsocketServerManager.TryGetOrCreateService(serverUri, out service))
{
    Console.WriteLine("Failed to create service");
    return;
}
else Console.WriteLine("Created service");

service.onOpen += (id) => Console.WriteLine(id);
service.onError += (id, error) => Console.WriteLine($"{id} {error.Message}");
service.onClose += (id, data) => Console.WriteLine($"{id} {data.Reason}");
service.onMessage += (id, data) => Console.WriteLine($"{id} {data.Data}");

Console.WriteLine("Press enter to exit...");
Console.ReadLine();

if (WebsocketServerManager.TryRemoveService(serverUri)) Console.WriteLine("Removed service");
else Console.WriteLine("Failed to remove service");
```