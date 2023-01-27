using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
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
#if NET6_0_OR_GREATER || NET48_OR_GREATER
        private readonly PipeSecurity? pipeSecurity;
#endif

        public string IPCName { get; private set; }
        public bool IsDisposed { get; private set; } = false;
        public IReadOnlyDictionary<Guid, PipeServer> PipeServers => pipeServers;
        public event Action<Guid>? OnConnect;
        //ReadOnlySpan would be faster and more efficent I believe but it cannot be used in this sceneario.
        public event Action<Guid, ReadOnlyMemory<byte>>? OnMessage;
        public event Action<Guid>? OnDispose;

        /// <summary>
        /// Creates a pipe server manager to handle multiple client connections.
        /// </summary>
        /// <param name="ipcName">The name to use for the pipe.</param>
        /// <param name="bufferSize">The buffer size for messages.</param>
        /// <param name="maxAllowedServerInstances">The maximum number of connections allowed to this server.</param>
        /// <param name="pipeSecurity">
        /// The security options for the the pipe server.
        /// <para>Only supported on <see cref="OSPlatform.Windows"/>.</para>
        /// </param>
        /// <exception cref="IOException"></exception>
        public PipeServerManager(string ipcName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances
#if NET6_0_OR_GREATER || NET48_OR_GREATER
            , PipeSecurity? pipeSecurity = null
#endif
            )
        {
            mutex = new Mutex(false, $"mutex_{ipcName}", out bool created);
            if (!created) throw new IOException($"A pipe server with the name '{ipcName}' already exists.");

            this.IPCName = ipcName;
            this.bufferSize = bufferSize;
            this.maxAllowedServerInstances = maxAllowedServerInstances;
#if NET6_0_OR_GREATER || NET48_OR_GREATER
            //The windows versions of the .NET framework and .NET core support extra security features for named pipes.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (pipeSecurity == null)
                    pipeSecurity = new PipeSecurity();

                //Allow this process to modify the pipe security.
                pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner!, PipeAccessRights.FullControl, AccessControlType.Allow));
                this.pipeSecurity = pipeSecurity;
            }
#endif

            CreateInstance();
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (IsDisposed) return;
                if (pendingPipeServer != null) pendingPipeServer.Dispose();
                foreach (PipeServer pipeServer in pipeServers.Values) pipeServer.Dispose();
                mutex.Dispose();
                IsDisposed = true;
            }
        }

        private void CreateInstance()
        {
            lock (lockObject)
            {
                if (IsDisposed || pendingPipeServer != null) return;
                else if (maxAllowedServerInstances > 0 && pipeServers.Count > maxAllowedServerInstances)
                    throw new IOException("The maximum number of server instances has been exceeded.");

                //Create the new pipe.
                PipeServer pipeServer = new(
                    IPCName, bufferSize,
                    maxAllowedServerInstances
#if NET6_0_OR_GREATER || NET48_OR_GREATER
                    , pipeSecurity
#endif
                    );
                pipeServer.OnConnect += PipeServer_OnConnect;

                pendingPipeServer = pipeServer;
            }
        }

        private void PipeServer_OnConnect()
        {
            //Check if we have disposed between the event firing and the disposal of this class.
            //No need to dispose of the pipe server here because the dispose method on this class will handle that.
            if (IsDisposed) return;

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
            pipeServer.OnMessage += (data) => OnMessage?.Invoke(guid, data);
            pipeServer.OnDispose += () => PipeServer_OnDispose(guid);

            //Fire the on-connection event for this new pipe.
            OnConnect?.Invoke(guid);
        }

        private void PipeServer_OnDispose(Guid guid)
        {
            OnDispose?.Invoke(guid);

            pipeServers.TryRemove(guid, out _);

            if (IsDisposed) return;
            
            if (pendingPipeServer == null)
            {
                try { CreateInstance(); }
                catch (IOException) { }
            }
        }

        public void SendMessage(Guid guid, ReadOnlyMemory<byte> data)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(PipeServerManager));

            if (!pipeServers.TryGetValue(guid, out PipeServer? pipeServer) || pipeServer == null) throw new Exception("Pipe server not found.");
            pipeServer.SendMessage(data);
        }

        public void BroadcastMessage(ReadOnlyMemory<byte> data)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(PipeServerManager));

            foreach (PipeServer pipeServer in pipeServers.Values)
                if (pipeServer.IsConnected)
                    pipeServer.SendMessage(data);
        }
    }
}
