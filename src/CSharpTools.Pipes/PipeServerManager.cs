using System.Collections.Concurrent;
using System.IO.Pipes;

namespace CSharpTools.Pipes
{
    public class PipeServerManager : IDisposable
    {
        private readonly object lockObject = new object();
        private readonly ConcurrentDictionary<Guid, PipeServer> pipeServers = new ConcurrentDictionary<Guid, PipeServer>();
        private int bufferSize { get; init; }
        private int maxAllowedServerInstances { get; init; }

        public string ipcName { get; init; }
        public bool isDisposed { get; private set; } = false;
        public ICollection<Guid> pipeServerIDs => pipeServers.Keys;
        public event Action<Guid>? OnConnect;
        //ReadOnlySpan would be faster and more efficent I believe but it cannot be used in this sceneario.
        public event Action<Guid, ReadOnlyMemory<byte>>? OnMessage;
        public event Action<Guid>? OnDispose;

        public PipeServerManager(string ipcName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances)
        {
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
                foreach (PipeServer pipeServer in pipeServers.Values) pipeServer.Dispose();
                isDisposed = true;
            }
        }

        private void CreateInstance()
        {
            lock (lockObject)
            {
                if (isDisposed) return;

                if (maxAllowedServerInstances > 0 && pipeServers.Count > maxAllowedServerInstances)
                    throw new IOException("The maximum number of server instances has been exceeded.");

                //Generate a unique ID for the new pipe.
                Guid guid;
                do guid = Guid.NewGuid();
                while (pipeServers.ContainsKey(guid));

                //Create the new pipe.
                PipeServer pipeServer = new(ipcName, bufferSize, maxAllowedServerInstances);
                pipeServer.OnConnect += () =>
                {
                    //Check if we have disposed between the event firing and the disposal of this class.
                    //No need to dispose of the pipe server here because the dispose method on this class will handle that.
                    if (isDisposed) return;
                    
                    //Create a new pipe before handling this one (to keep connection times down).
                    CreateInstance();

                    //Adding the event listners here prevents the temporary pipe (the one waiting for a new connection) from firing events.
                    pipeServer.OnMessage += (data) => OnMessage?.Invoke(guid, data);
                    pipeServer.OnDispose += () => OnDispose?.Invoke(guid);

                    //Fire the on-connection event for this new pipe.
                    OnConnect?.Invoke(guid);
                };

                //Ideally I should store this new pipe in a seperate variable until it gets a connection.
                if (!pipeServers.TryAdd(guid, pipeServer))
                {
                    pipeServer.Dispose();
                    throw new Exception("Unable to add new pipe server.");
                }
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
