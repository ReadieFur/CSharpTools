using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    internal class WebsocketServerHelper
    {
        public WebSocketServer websocketServer { get; private set; }
        public Dictionary<string, WebsocketServiceWrapper> services { get; private set; } = new Dictionary<string, WebsocketServiceWrapper>();

        public WebsocketServerHelper(WebSocketServer websocketServer) => this.websocketServer = websocketServer;
    }
}
