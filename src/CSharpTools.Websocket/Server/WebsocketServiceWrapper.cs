using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace CSharpTools.Websocket.Server
{
    public class WebsocketServiceWrapper
    {
        private object queuedSendMutexObject = new object();
        private bool processingMessages = false;
        private ConcurrentQueue<(string, Action)> queuedMessages = new ConcurrentQueue<(string, Action)>();

        private ConcurrentDictionary<string, (object, bool, ConcurrentQueue<(string, Action<bool>)>)> queuedIndividualMessages =
            new ConcurrentDictionary<string, (object, bool, ConcurrentQueue<(string, Action<bool>)>)>();

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

        public void Broadcast(object message, bool async = false, Action completed = null)
        {
            string strMessage = message.ToString();
            if (async) webSocketServiceManager.Sessions.BroadcastAsync(strMessage, completed);
            else
            {
                webSocketServiceManager.Sessions.Broadcast(strMessage);
                completed();
            }
        }

        public bool BroadcastQueueSend(object message, Action completed = null, int millisecondsTimeout = 100)
        {
            if (webSocketServiceManager.Sessions.Count == 0) return true;

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
                    while (webSocketServiceManager.Sessions.Count > 0 && queuedMessages.Count > 0)
                    {
                        if (queuedMessages.TryDequeue(out var messageToSend))
                        {
                            webSocketServiceManager.Sessions.Broadcast(messageToSend.Item1);
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

        public bool Send(string id, object message, bool async = false, Action<bool> callback = null)
        {
            IWebSocketSession session;
            if (!webSocketServiceManager.Sessions.TryGetSession(id, out session)) return false;

            string strMessage = message.ToString();

            if (async) session.Context.WebSocket.SendAsync(strMessage, callback);
            else
            {
                session.Context.WebSocket.Send(strMessage);
                if (callback != null) callback(true);
            }

            return true;
        }

        public bool QueueSend(string id, object message, Action<bool> callback = null, int millisecondsTimeout = 100)
        {
            IWebSocketSession session;
            if (webSocketServiceManager.Sessions.Count == 0
                || !webSocketServiceManager.Sessions.TryGetSession(id, out session)) return false;

            var queuedDataForID = queuedIndividualMessages.GetOrAdd(id, (new object(), false, new ConcurrentQueue<(string, Action<bool>)>()));
            queuedDataForID.Item3.Enqueue((message.ToString(), callback));

            if (!Monitor.TryEnter(queuedDataForID.Item1, millisecondsTimeout)) return false;

            if (queuedDataForID.Item3.Count > 0)
            {
                queuedDataForID.Item2 = true;
                Task.Run(() =>
                {
                    while (webSocketServiceManager.Sessions.TryGetSession(id, out _) && queuedDataForID.Item3.Count > 0)
                    {
                        if (queuedDataForID.Item3.TryDequeue(out var messageToSend))
                        {
                            session.Context.WebSocket.Send(messageToSend.Item1);
                            if (messageToSend.Item2 != null) Task.Run(() => messageToSend.Item2(true));
                        }
                    }

                    lock (queuedDataForID.Item1)
                    {
                        if (!webSocketServiceManager.Sessions.TryGetSession(id, out _) || queuedDataForID.Item3.Count == 0)
                            queuedIndividualMessages.TryRemove(id, out _);
                        else queuedDataForID.Item2 = false;
                    }
                });
            }

            Monitor.Exit(queuedSendMutexObject);
            return true;
        }
    }
}
