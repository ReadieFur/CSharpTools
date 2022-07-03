using CSharpTools.Websocket.Server;
using CSharpTools.Websocket.Client;

namespace CSharpTools.Tests.Websocket
{
    internal class ClientAndServerT
    {
        private static Uri uri = new Uri("ws://127.0.0.1:8080/");
        private static WebsocketServiceWrapper serverService;
        private static WebsocketClientHelper clientWebsocket;

        public static void Main()
        {
            //Setup connections.
            if (!Server()) throw new Exception("Failed to create server.");
            if (!Client()) throw new Exception("Failed to create client.");

            serverService.BroadcastQueueSend("Server to client test queue message.");
            clientWebsocket.QueueSend("Client to server test queue message.");

            //Wait for user input to exit.
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();

            //Shutdown connections.
            clientWebsocket.Close();
            Console.WriteLine("Removed websocket");

            if (WebsocketServerManager.TryRemoveService(uri)) Console.WriteLine("Removed service");
            else Console.WriteLine("Failed to remove service");
        }

        private static bool Server()
        {
            if (!WebsocketServerManager.TryGetOrCreateService(uri, out serverService))
            {
                Console.WriteLine("Failed to create service");
                return false;
            }
            else Console.WriteLine("Created service");

            serverService.onOpen += (id) =>
            {
                Console.WriteLine($"Server onOpen {id}");
                serverService.Send(id, "Hello Client!");
            };
            serverService.onError += (id, error) => Console.WriteLine($"Server onError {id} {error.Message}");
            serverService.onClose += (id, data) => Console.WriteLine($"Server onClose {id} {data.Reason}");
            serverService.onMessage += (id, data) => Console.WriteLine($"Server onMessage {id} {data.Data}");
            serverService.onDispose += () => Console.WriteLine("Server onDispose");

            //serverService.BroadcastQueueSend("Server to client test queue message");

            return true;
        }

        private static bool Client()
        {
            clientWebsocket = new WebsocketClientHelper(uri);
            Console.WriteLine("Created websocket");

            clientWebsocket.Connect();

            clientWebsocket.OnOpen += (s, e) =>
            {
                Console.WriteLine("Client onOpen");
                clientWebsocket.Send("Hello Server!");
            };
            clientWebsocket.OnError += (s, error) => Console.WriteLine($"Client onError {error.Message}");
            clientWebsocket.OnClose += (s, data) => Console.WriteLine($"Client onClose {data.Reason}");
            clientWebsocket.OnMessage += (s, data) => Console.WriteLine($"Client onMessage {data.Data}");

            return true;
        }
    }
}
