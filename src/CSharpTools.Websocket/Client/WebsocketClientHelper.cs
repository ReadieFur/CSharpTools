using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using WebSocketSharp;

namespace CSharpTools.Websocket.Client
{
    public class WebsocketClientHelper : WebSocket
    {
        private bool shouldClose = false;
        private object queuedSendMutexObject = new object();
        private bool processingMessages = false;
        private ConcurrentQueue<(string, Action)> queuedMessages = new ConcurrentQueue<(string, Action)>();

        public Uri uri { get; private set; }
        public bool autoReconnect = true;

        public WebsocketClientHelper(Uri uri) : base(uri.ToString())
        {
            this.uri = uri;
            OnClose += WebsocketClientHelper_OnClose;
        }

        private void WebsocketClientHelper_OnClose(object sender, CloseEventArgs e)
        {
            if (shouldClose || !autoReconnect) return;
            Connect();
        }

        //https://stackoverflow.com/questions/1565734/is-it-possible-to-set-private-property-via-reflection
        private void ResetConnectionAttemptsCount()
        {
            object obj = this;
            string propName = "_retryCountForConnect";
            int val = 0;
            
            Type type = obj.GetType();
            FieldInfo fieldInfo = null;
            while (fieldInfo == null && type != null)
            {
                fieldInfo = type.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            if (fieldInfo == null) throw new ArgumentOutOfRangeException("propName", $"Field {propName} was not found in Type {obj.GetType().FullName}");
            fieldInfo.SetValue(obj, val);
        }

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
                    while (ReadyState == WebSocketState.Open && queuedMessages.Count > 0)
                    {
                        if (queuedMessages.TryDequeue(out var messageToSend))
                        {
                            Send(messageToSend.Item1);
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

        new public void Connect()
        {
            ResetConnectionAttemptsCount();
            base.Connect();
        }

        new public void ConnectAsync()
        {
            ResetConnectionAttemptsCount();
            base.ConnectAsync();
        }

        public void Close()
        {
            shouldClose = true;
            base.Close();
        }

        public void Close(ushort code)
        {
            shouldClose = true;
            base.Close(code);
        }

        public void Close(CloseStatusCode code)
        {
            shouldClose = true;
            base.Close(code);
        }

        public void Close(ushort code, string reason)
        {
            shouldClose = true;
            base.Close(code, reason);
        }

        public void Close(CloseStatusCode code, string reason)
        {
            shouldClose = true;
            base.Close(code, reason);
        }

        public void CloseAsync()
        {
            shouldClose = true;
            base.CloseAsync();
        }

        public void CloseAsync(ushort code)
        {
            shouldClose = true;
            base.CloseAsync(code);
        }

        public void CloseAsync(CloseStatusCode code)
        {
            shouldClose = true;
            base.CloseAsync(code);
        }

        public void CloseAsync(ushort code, string reason)
        {
            shouldClose = true;
            base.CloseAsync(code, reason);
        }

        public void CloseAsync(CloseStatusCode code, string reason)
        {
            shouldClose = true;
            base.CloseAsync(code, reason);
        }
    }
}
