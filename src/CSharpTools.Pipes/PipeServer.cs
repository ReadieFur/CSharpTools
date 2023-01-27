using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace CSharpTools.Pipes
{
    public class PipeServer : APipe<NamedPipeServerStream>
    {
        protected override PipeStream _pipe { get; set; }

        /// <summary>
        /// Creates a pipe server instance to handle a single client connection.
        /// <para>Should be used in conjunction with <see cref="PipeServerManager"/>.</para>
        /// </summary>
        /// <param name="pipeName">The name to use for the pipe.</param>
        /// <param name="bufferSize">The buffer size for messages.</param>
        /// <param name="maxAllowedServerInstances">The maximum number of connections allowed to this server.</param>
        /// <param name="pipeSecurity">
        /// The security options for the the pipe server.
        /// <para>Only supported on <see cref="OSPlatform.Windows"/>.</para>
        /// </param>
        public PipeServer(string pipeName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances
#if NET6_0_OR_GREATER || NET48_OR_GREATER
            , PipeSecurity? pipeSecurity = null
#endif
            )
            : base(pipeName, bufferSize)
        {
#if NET6_0_OR_GREATER || NET48_OR_GREATER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && pipeSecurity != null)
            {
#if NET6_0_OR_GREATER
                _pipe = NamedPipeServerStreamAcl.Create(
                    pipeName,
                    PipeDirection.InOut,
                    maxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    bufferSize,
                    bufferSize,
                    pipeSecurity);
#elif NET48_OR_GREATER
                _pipe = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    maxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    bufferSize,
                    bufferSize,
                    pipeSecurity);
#endif
            }
            else
            {
                _pipe = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    maxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
            }
#else
                _pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                maxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
#endif

            pipe.BeginWaitForConnection(OnConnectCallback, null);
        }

        private void OnConnectCallback(IAsyncResult asyncCallback)
        {
            //System.IO.IOException
            try { pipe.EndWaitForConnection(asyncCallback); }
            catch (IOException) { Dispose(); }
            catch (ObjectDisposedException) { Dispose(); }
            OnConnectCallback();
        }
    }
}
