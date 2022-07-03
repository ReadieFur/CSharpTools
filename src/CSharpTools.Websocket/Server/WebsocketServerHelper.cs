using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    internal class WebsocketServerHelper
    {
        public WebSocketServer websocketServer { get; private set; }
        public Dictionary<string, WebsocketServiceHelper> services { get; private set; } = new Dictionary<string, WebsocketServiceHelper>();

        public WebsocketServerHelper(WebSocketServer websocketServer) => this.websocketServer = websocketServer;
    }
}
