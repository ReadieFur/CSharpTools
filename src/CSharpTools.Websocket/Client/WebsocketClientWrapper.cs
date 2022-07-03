using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace CSharpTools.Websocket.Client
{
    public class WebsocketClientWrapper
    {
        private bool isFirstConnection = true;
        private WebSocket websocket;
        private bool shouldClose = false;
        private object queuedSendMutexObject = new object();
        private bool processingMessages = false;
        private ConcurrentQueue<(string, Action)> queuedMessages = new ConcurrentQueue<(string, Action)>();

        public Uri uri { get; private set; }

        public event Action onOpen;
        public event Action<CloseEventArgs> onClose;
        public event Action<ErrorEventArgs> onError;
        public event Action<MessageEventArgs> onMessage;
        public event Action onDispose;

        internal WebsocketClientWrapper(Uri uri) => this.uri = uri;

        internal bool Connect()
        {
            try { websocket = new WebSocket(uri.ToString()); }
            catch { return false; }
            websocket.Connect();

#if DEBUG
            websocket.Log.Level = LogLevel.Trace;
#endif

            websocket.OnOpen += Websocket_OnOpen;
            websocket.OnClose += Websocket_OnClose;
            websocket.OnError += Websocket_OnError;
            websocket.OnMessage += Websocket_OnMessage;

            //If this is the first connection we are making, send an artificial onOpen message as the first one will be missed dusing this construction.
            //Alternatively the caller of this function can run whatever code is needed if this method returns true.
            if (isFirstConnection) Task.Run(() =>
            {
                isFirstConnection = false;
                //We shouldn't have to wait more than 100 ms to send this artificial onOpen event.
                Thread.Sleep(100);
                onOpen?.Invoke();
            });

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
            onClose?.Invoke(e);
            if (!shouldClose)
            {
                Thread.Sleep(1000);
                Connect();
            }
        }

        private void Websocket_OnError(object sender, ErrorEventArgs e) => onError?.Invoke(e);

        private void Websocket_OnMessage(object sender, MessageEventArgs e) => onMessage?.Invoke(e);

        public bool QueueSend(object message, Action completed = null, int millisecondsTimeout = 100)
        {
            queuedMessages.Enqueue((message.ToString(), completed));

            if (processingMessages) return true;
            
            //Lock while we check the count.
            if (!Monitor.TryEnter(queuedSendMutexObject, millisecondsTimeout)) return false;

            //setup the task if needed.
            if (queuedMessages.Count > 0)
            {
                processingMessages = true;
                //Leave this task to run and let the rest of this method continue.
                Task.Run(() =>
                {
                    while (isAlive && queuedMessages.Count > 0)
                    {
                        if (queuedMessages.TryDequeue(out var messageToSend))
                        {
                            websocket.Send(messageToSend.Item1);
                            if (messageToSend.Item2 != null)
                            {
                                //Run the callback task without halting this loop.
                                Task.Run(() => messageToSend.Item2());
                            }
                        }
                    }
                }).ContinueWith(_ => processingMessages = false);
            }

            Monitor.Exit(queuedSendMutexObject);
            return true;
        }

        #region Base public members
        public CompressionMethod compression => websocket.Compression;
        public IEnumerable<Cookie> cookies => websocket.Cookies;
        public NetworkCredential credentials => websocket.Credentials;
        public bool emitOnPing => websocket.EmitOnPing;
        public bool enableRedirection => websocket.EnableRedirection;
        public string extensions => websocket.Extensions;
        public bool isAlive => websocket.IsAlive;
        public bool isSecure => websocket.IsSecure;
        public Logger log => websocket.Log;
        public string origin => websocket.Origin;
        public string protocol => websocket.Protocol;
        public WebSocketState readyState => websocket.ReadyState;
        public ClientSslConfiguration sslConfiguration => websocket.SslConfiguration;
        public TimeSpan waitTime => websocket.WaitTime;
        public bool Ping() => websocket.Ping();
        public bool Ping(string message) => websocket.Ping();
        public void Send(byte[] data) => websocket.Send(data);
        public void Send(string message) => websocket.Send(message);
        public void SendAsync(byte[] data, Action<bool> completed) => websocket.SendAsync(data, completed);
        public void SendAsync(string message, Action<bool> completed) => websocket.SendAsync(message, completed);
        public void SetCookie(Cookie cookie) => websocket.SetCookie(cookie);
        public void SetCredentials(string username, string password, bool preAuth) => websocket.SetCredentials(username, password, preAuth);
        public void SetProxy(string url, string username, string password) => websocket.SetProxy(url, username, password);
        #endregion
    }
}
