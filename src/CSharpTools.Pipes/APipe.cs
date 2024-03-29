﻿//#define USE_MESSAGE_MODE

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace CSharpTools.Pipes
{
    public abstract class APipe<TPipe> : IDisposable where TPipe : PipeStream
    {
        protected readonly object lockObject = new object();
        protected abstract PipeStream _pipe { get; set; }
        protected virtual TPipe pipe { get => (TPipe)_pipe; }
        protected byte[] buffer { get; set; }
        //protected abstract ReadOnlyMemory<byte> lastData { get; set; }
        protected readonly ManualResetEventSlim connectedResetEvent = new ManualResetEventSlim();
        public bool IsConnected { get => pipe.IsConnected; }
        public bool IsDisposed { get; protected set; } = false;

        public string IPCName { get; private set; }
        public int BufferSize { get; private set; }
        public event Action? OnConnect;
        public event Action<ReadOnlyMemory<byte>>? OnMessage;
        public event Action? OnDispose;

        public APipe(string ipcName, int bufferSize)
        {
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "The buffer size must be at least 1.");

            this.IPCName = ipcName;
            this.BufferSize = bufferSize;

            buffer = new byte[bufferSize];
        }

        public virtual void Dispose()
        {
            lock (lockObject)
            {
                if (IsDisposed) return;
                OnDispose?.Invoke();
                _pipe.Close();
                _pipe.Dispose();
                IsDisposed = true;
            }
        }

        protected virtual void OnConnectCallback()
        {
            connectedResetEvent.Set();
            OnConnect?.Invoke();
            BeginRead();
        }

        protected virtual void BeginRead()
        {
            try { _pipe.BeginRead(buffer, 0, BufferSize, OnEndReadCallback, null); }
            catch (ObjectDisposedException) { Dispose(); }
        }

        protected virtual void OnEndReadCallback(IAsyncResult asyncResult)
        {
            int bytesRead = _pipe.EndRead(asyncResult);
            if (bytesRead <= 0)
            {
                //If no bytes were read then the connection has been closed.
                Dispose();
                return;
            }

#if USE_MESSAGE_MODE
            //https://github.com/IfatChitin/Named-Pipes
            if (!pipeServer.IsMessageComplete)
            {
                //Continue reading if we haven't got the full message.
                BeginRead();
                return;
            }
#else
            //This seems to work but as far as I'm aware it shouldn't/isn't safe incase we only recieve part of a message.
            //Leading me to believe that in my tests I was always recieving the full message in one go.
#endif

            ReadOnlyMemory<byte> data = new ReadOnlyMemory<byte>(buffer);

            //Update the last stored data.
            //lastData = data;

            //Dispatch the OnMessage event.
            OnMessage?.Invoke(data);

            //Clear the byte stream for new messages.
            buffer = new byte[BufferSize];

            //Continue to the next read
            BeginRead();
        }
        
        public virtual void WaitForConnection(int millisecondsTimeout = -1) => connectedResetEvent.Wait(millisecondsTimeout);

        public async virtual void SendMessage(ReadOnlyMemory<byte> data)
        {
            if (!_pipe.IsConnected) throw new IOException("The pipe is not connected.");
            else if (data.Length > BufferSize) throw new IOException("The message is too large.");
            else if (data.Length == 0) throw new IOException("The message is empty.");
            else if (data.Length > BufferSize) throw new IOException("The message is too large.");

            await _pipe.WriteAsync(data.ToArray(), 0, BufferSize);
        }
    }
}
