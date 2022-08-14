# CSharpTools.Pipes
This library contains a helper class to make using shared memory and buffers in C# easier.  

## Installation:
Grab the latest build from the releases folder [here](./bin/Release/netstandard2.0/CSharpTools.Pipes.dll).  
Once you have done that, add the dll to your projects refrence files.

## Example:
**Note:** The numbers used here in the seralize and deserialize methods are arbitrary and could be any serializable struct.
```cs
string ipcName = "test_pipe";
int bufferSize = Helpers.ComputeBufferSizeOf<int>();

//Server setup
PipeServerManager pipeServerManager = new(ipcName, bufferSize);
pipeServerManager.OnConnect += (id) => Console.WriteLine($"SERVER: New client '{id}'");
pipeServerManager.OnMessage += (id, data) =>
{
    int formattedData = Helpers.Deserialize<int>(data.ToArray());
    Console.WriteLine($"SERVER: ({id}) '{formattedData}'");
};
pipeServerManager.OnDispose += (id) => Console.WriteLine($"SERVER: Client disconnected '{id}'");

//Client setup
PipeClient client = new(ipcName, bufferSize);
client.OnConnect += () => Console.WriteLine("CLIENT: Connected");
client.OnMessage += (data) =>
{
    int formattedData = Helpers.Deserialize<int>(data.ToArray());
    Console.WriteLine($"CLIENT: ({i}) '{formattedData}'");
}
client.OnDispose += () => Console.WriteLine("CLIENT: Disconnected");

//Server messaging
pipeServerManager.BroadcastMessage(Helpers.Serialize(pipeServerManager.pipeServerIDs.Count));
pipeServerManager.SendMessage(pipeServerManager.pipeServerIDs.First(), Helpers.Serialize(4));

//Client messaging
client.SendMessage(Helpers.Serialize(5));

//Cleanup
client.Dispose();
pipeServerManager.Dispose();
```
