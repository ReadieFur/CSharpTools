using System;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    public class WebsocketService : WebSocketBehavior
    {
        internal static event Action<Uri, string, WebsocketService> onOpen;
        public event Action<WebSocketSharp.CloseEventArgs> onClose;
        public event Action<WebSocketSharp.ErrorEventArgs> onError;
        public event Action<WebSocketSharp.MessageEventArgs> onMessage;

        protected override void OnOpen() => onOpen?.Invoke(Context.RequestUri, ID, this);

        protected override void OnClose(WebSocketSharp.CloseEventArgs e) => onClose?.Invoke(e);

        protected override void OnError(WebSocketSharp.ErrorEventArgs e) => onError?.Invoke(e);

        protected override void OnMessage(WebSocketSharp.MessageEventArgs e) => onMessage?.Invoke(e);
    }
}
