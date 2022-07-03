using System;
using System.Collections.Generic;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    public class WebsocketServiceWrapper
    {
        public Uri hostUri { get; private set; }
        public WebSocketServiceHost webSocketServiceManager { get; private set; }
        public Dictionary<string, WebsocketService> websocketServices { get; private set; } = new Dictionary<string, WebsocketService>();

        public event Action onDispose;
        public event Action<string> onOpen;
        public event Action<string, WebSocketSharp.CloseEventArgs> onClose;
        public event Action<string, WebSocketSharp.ErrorEventArgs> onError;
        public event Action<string, WebSocketSharp.MessageEventArgs> onMessage;

        internal WebsocketServiceWrapper(Uri hostUri, WebSocketServiceHost webSocketServiceManager)
        {
            this.hostUri = hostUri;
            this.webSocketServiceManager = webSocketServiceManager;

            WebsocketService.onOpen += WebsocketService_onOpen;
        }

        internal void Dispose() => onDispose?.Invoke();

        private void WebsocketService_onOpen(Uri context, string id, WebsocketService websocketService)
        {
            if (websocketServices.ContainsKey(id)) return;

            if (context.Scheme != hostUri.Scheme
                || context.Port != hostUri.Port
                || context.AbsolutePath != hostUri.AbsolutePath) return;

            string serverIP = hostUri.Host == "localhost" ? "127.0.0.1" : hostUri.Host;
            string contextIP = context.Host == "localhost" ? "127.0.0.1" : context.Host;
            if (serverIP != "0.0.0.0" && serverIP != contextIP) return;

            websocketServices.Add(id, websocketService);

            //I would've like to have a way to unsubscribe from the events here but
            //that dosen't seem to be possible with lambdas (at least not the onClose).
            Action<WebSocketSharp.ErrorEventArgs> onErrorCallback = (e) => onError?.Invoke(id, e);
            Action<WebSocketSharp.MessageEventArgs> onMessageCallback = (e) => onMessage?.Invoke(id, e);
            websocketService.onError += onErrorCallback;
            websocketService.onMessage += onMessageCallback;
            websocketService.onClose += (e) =>
            {
                onClose?.Invoke(id, e);

                websocketService.onError -= onErrorCallback;
                websocketService.onMessage -= onMessageCallback;

                websocketServices.Remove(id);
            };

            onOpen?.Invoke(id);
        }

        public void BroadcastAsync(object message, Action completed = default) =>
            webSocketServiceManager.Sessions.BroadcastAsync(message.ToString(), completed);
        public void Broadcast(object message) => webSocketServiceManager.Sessions.Broadcast(message.ToString());
    }
}
