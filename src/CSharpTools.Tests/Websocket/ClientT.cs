using CSharpTools.Websocket.Client;

namespace CSharpTools.Tests.Websocket
{
    internal class ClientT
    {
        public static void Main()
        {
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
        }
    }
}
