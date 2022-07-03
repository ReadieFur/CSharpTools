using CSharpTools.Websocket.Client;

namespace CSharpTools.Tests.Websocket
{
    internal class ClientT
    {
        public static void Main()
        {
            Uri uri = new Uri("ws://127.0.0.1:8080/");
            WebsocketClientHelper websocket = new WebsocketClientHelper(uri) { autoReconnect = false };
            Console.WriteLine("Created websocket");

            websocket.Connect();

            websocket.OnOpen += (s, e) => Console.WriteLine("Client onOpen");
            websocket.OnError += (s, error) => Console.WriteLine($"Client onError {error.Message}");
            websocket.OnClose += (s, data) => Console.WriteLine($"Client onClose {data.Reason}");
            websocket.OnMessage += (s, data) => Console.WriteLine($"Client onMessage {data.Data}");

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();

            websocket.Close();
            Console.WriteLine("Removed websocket");
        }
    }
}
