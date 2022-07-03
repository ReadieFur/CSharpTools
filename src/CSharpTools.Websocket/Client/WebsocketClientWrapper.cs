using System;
using System.Threading;
using WebSocketSharp;

namespace CSharpTools.Websocket.Client
{
    public class WebsocketClientWrapper
    {
        private WebSocket websocket;
        private bool shouldClose = false;

        public Uri uri { get; private set; }
        public bool isConnected { get; private set; }

        public event Action onOpen;
        public event Action<CloseEventArgs> onClose;
        public event Action<WebSocketSharp.ErrorEventArgs> onError;
        public event Action<MessageEventArgs> onMessage;
        public event Action onDispose;

        internal WebsocketClientWrapper(Uri uri) => this.uri = uri;

        internal bool Connect()
        {
            try { websocket = new WebSocket(uri.ToString()); }
            catch { return false; }

#if DEBUG
            websocket.Log.Level = WebSocketSharp.LogLevel.Trace;
#endif

            websocket.OnOpen += Websocket_OnOpen;
            websocket.OnClose += Websocket_OnClose;
            websocket.OnError += Websocket_OnError;
            websocket.OnMessage += Websocket_OnMessage;

            isConnected = true;
            return true;
        }

        internal void Dispose()
        {
            onDispose?.Invoke();

            websocket.OnOpen -= Websocket_OnOpen;
            websocket.OnClose -= Websocket_OnClose;
            websocket.OnError -= Websocket_OnError;
            websocket.OnMessage -= Websocket_OnMessage;

            shouldClose = true;
            websocket.Close();
        }

        private void Websocket_OnOpen(object sender, EventArgs e) => onOpen?.Invoke();

        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
            isConnected = false;
            onClose?.Invoke(e);
            if (!shouldClose)
            {
                Thread.Sleep(1000);
                Connect();
            }
        }

        private void Websocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e) => onError?.Invoke(e);

        private void Websocket_OnMessage(object sender, MessageEventArgs e) => onMessage?.Invoke(e);
    }
}
