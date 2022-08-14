using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace CSharpTools.Pipes
{
    public class PipeServerManager : IDisposable
    {
        private readonly object lockObject = new object();
        private Mutex mutex;
        private readonly ConcurrentDictionary<Guid, PipeServer> pipeServers = new ConcurrentDictionary<Guid, PipeServer>();
        private PipeServer? pendingPipeServer;
        private readonly int bufferSize;
        private readonly int maxAllowedServerInstances;
        
        public string ipcName { get; private set; }
        public bool isDisposed { get; private set; } = false;
        public ICollection<Guid> pipeServerIDs => pipeServers.Keys;
        public event Action<Guid>? onConnect;
        //ReadOnlySpan would be faster and more efficent I believe but it cannot be used in this sceneario.
        public event Action<Guid, ReadOnlyMemory<byte>>? onMessage;
        public event Action<Guid>? onDispose;

        public PipeServerManager(string ipcName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances)
        {
            mutex = new Mutex(false, $"mutex_ipcName", out bool created);
            if (!created) throw new IOException($"A pipe server with the name '{ipcName}' already exists.");

            this.ipcName = ipcName;
            this.bufferSize = bufferSize;
            this.maxAllowedServerInstances = maxAllowedServerInstances;

            CreateInstance();
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (isDisposed) return;
                if (pendingPipeServer != null) pendingPipeServer.Dispose();
                foreach (PipeServer pipeServer in pipeServers.Values) pipeServer.Dispose();
                mutex.Dispose();
                isDisposed = true;
            }
        }

        private void CreateInstance()
        {
            lock (lockObject)
            {
                if (isDisposed || pendingPipeServer != null) return;
                else if (maxAllowedServerInstances > 0 && pipeServers.Count > maxAllowedServerInstances)
                    throw new IOException("The maximum number of server instances has been exceeded.");

                //Create the new pipe.
                PipeServer pipeServer = new(ipcName, bufferSize, maxAllowedServerInstances);
                pipeServer.onConnect += PipeServer_OnConnect;

                pendingPipeServer = pipeServer;
            }
        }

        private void PipeServer_OnConnect()
        {
            //Check if we have disposed between the event firing and the disposal of this class.
            //No need to dispose of the pipe server here because the dispose method on this class will handle that.
            if (isDisposed) return;

            //Generate a unique ID for the new pipe.
            Guid guid;
            do guid = Guid.NewGuid();
            while (pipeServers.ContainsKey(guid));

            PipeServer pipeServer = pendingPipeServer!;
            if (!pipeServers.TryAdd(guid, pipeServer))
            {
                pipeServer.Dispose();
                throw new Exception("Unable to add new pipe server.");
            }
            pendingPipeServer = null;

            //Create a new pipe before handling this one (to keep connection times down).
            try { CreateInstance(); }
            catch (IOException) { }

            //Adding the event listners here prevents the temporary pipe (the one waiting for a new connection) from firing events.
            pipeServer.onMessage += (data) => onMessage?.Invoke(guid, data);
            pipeServer.onDispose += () => PipeServer_OnDispose(guid);

            //Fire the on-connection event for this new pipe.
            onConnect?.Invoke(guid);
        }

        private void PipeServer_OnDispose(Guid guid)
        {
            onDispose?.Invoke(guid);

            pipeServers.TryRemove(guid, out _);

            if (isDisposed) return;
            
            if (pendingPipeServer == null)
            {
                try { CreateInstance(); }
                catch (IOException) { }
            }
        }

        public void SendMessage(Guid guid, ReadOnlyMemory<byte> data)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(PipeServerManager));

            if (!pipeServers.TryGetValue(guid, out PipeServer? pipeServer) || pipeServer == null) throw new Exception("Pipe server not found.");
            pipeServer.SendMessage(data);
        }

        public void BroadcastMessage(ReadOnlyMemory<byte> data)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(PipeServerManager));

            foreach (PipeServer pipeServer in pipeServers.Values)
                if (pipeServer.isConnected)
                    pipeServer.SendMessage(data);
        }
    }
}
