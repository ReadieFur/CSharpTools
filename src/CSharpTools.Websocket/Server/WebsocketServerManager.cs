using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    public static class WebsocketServerManager
    {
        private static readonly object mutexObject = new object();
        private static Dictionary<string, WebsocketServerHelper> websocketServers = new Dictionary<string, WebsocketServerHelper>();

        public static bool TryGetOrCreateService(string host, int port, string path, out WebsocketServiceWrapper websocketServiceHelper) =>
            TryGetOrCreateService(new Uri($"ws://{host}:{port}{path}"), out websocketServiceHelper);

        public static bool TryGetOrCreateService(Uri uri, out WebsocketServiceWrapper websocketServiceHelper)
        {
            string address = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            string path = uri.AbsolutePath;

            lock (mutexObject)
            {
                if (websocketServers.TryGetValue(address, out WebsocketServerHelper server))
                {
                    //The server must be started for the services to not be null.
                    if (server.websocketServer.IsListening)
                    {
                        server.websocketServer.Start();
                        if (!server.websocketServer.IsListening)
                        {
                            websocketServiceHelper = null;
                            return false;
                        }
                    }

                    if (server.services.TryGetValue(path, out WebsocketServiceWrapper service))
                    {
                        //Return service.
                        websocketServiceHelper = service;
                        return true;
                    }
                    else
                    {
                        //Create service.
                        server.websocketServer.AddWebSocketService<WebsocketService>(path);
                        if (server.websocketServer.WebSocketServices[path] == null)
                        {
                            websocketServiceHelper = null;
                            return false;
                        }
                        websocketServiceHelper = new WebsocketServiceWrapper(uri, server.websocketServer.WebSocketServices[path]);
                        server.services.Add(path, websocketServiceHelper);
                        return true;
                    }
                }
                else
                {
                    //Create server and host.
                    WebSocketServer websocketServer = new WebSocketServer(address);
#if DEBUG
                    websocketServer.Log.Level = WebSocketSharp.LogLevel.Trace;
#endif
                    websocketServer.Start();
                    if (!websocketServer.IsListening)
                    {
                        websocketServiceHelper = null;
                        return false;
                    }

                    websocketServers.Add(address, new WebsocketServerHelper(websocketServer));

                    websocketServer.AddWebSocketService<WebsocketService>(path);
                    if (websocketServer.WebSocketServices[path] == null)
                    {
                        websocketServiceHelper = null;
                        return false;
                    }
                    websocketServiceHelper = new WebsocketServiceWrapper(uri, websocketServer.WebSocketServices[path]);
                    websocketServers[address].services.Add(path, websocketServiceHelper);
                    websocketServer.Log.Trace($"Endpoint added at: {websocketServiceHelper.hostUri}");

                    return true;
                }
            }
        }

        public static bool TryRemoveService(string host, int port, string path) => TryRemoveService(new Uri($"ws://{host}:{port}{path}"));

        public static bool TryRemoveService(Uri uri)
        {
            lock (mutexObject)
            {
                string address = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
                string path = uri.AbsolutePath;

                if (websocketServers.TryGetValue(address, out WebsocketServerHelper server))
                {
                    if (server.services.TryGetValue(path, out WebsocketServiceWrapper service))
                    {
                        service.Dispose();
                        server.services.Remove(path);
                    }

                    if (server.services.Count == 0)
                    {
                        server.websocketServer.Stop();
                        websocketServers.Remove(address);
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
