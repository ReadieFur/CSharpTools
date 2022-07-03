using System;
using System.Collections.Generic;

namespace CSharpTools.Websocket.Client
{
    public class WebsocketClientManager
    {
        private static readonly object mutexObject = new object();
        private static Dictionary<Uri, WebsocketClientWrapper> websockets = new Dictionary<Uri, WebsocketClientWrapper>();

        public static bool TryGetOrCreateConnection(string host, int port, string path, out WebsocketClientWrapper websocketWrapper) =>
            TryGetOrCreateConnection(new Uri($"ws://{host}:{port}{path}"), out websocketWrapper);

        public static bool TryGetOrCreateConnection(Uri uri, out WebsocketClientWrapper websocketWrapper)
        {
            lock (mutexObject)
            {
                if (websockets.TryGetValue(uri, out websocketWrapper))
                {
                    if (!websocketWrapper.isConnected && !websocketWrapper.Connect())
                    {
                        websocketWrapper = null;
                        TryRemoveConnection(uri);
                        return false;
                    }
                    return true;
                }

                websocketWrapper = new WebsocketClientWrapper(uri);
                if (!websocketWrapper.Connect())
                {
                    websocketWrapper = null;
                    return false;
                }

                websockets.Add(uri, websocketWrapper);
                return true;
            }
        }

        public static bool TryRemoveConnection(string host, int port, string path) => TryRemoveConnection(new Uri($"ws://{host}:{port}{path}"));

        public static bool TryRemoveConnection(Uri uri)
        {
            lock (mutexObject)
            {
                if (websockets.TryGetValue(uri, out WebsocketClientWrapper websocket))
                {
                    websocket.Dispose();
                    return true;
                }
                return false;
            }
        }
    }
}
