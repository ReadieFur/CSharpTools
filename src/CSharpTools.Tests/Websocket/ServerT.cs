using CSharpTools.Websocket.Server;

namespace CSharpTools.Tests.Websocket
{
    internal class ServerT
    {
        public static void Main()
        {
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
        }
    }
}
