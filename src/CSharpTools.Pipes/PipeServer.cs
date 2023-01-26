//#define USE_MESSAGE_MODE

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CSharpTools.Pipes
{
    public class PipeServer : APipe<NamedPipeServerStream>
    {
        protected override PipeStream _pipe { get; set; }

        public PipeServer(string pipeName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances
#if NET6_0_OR_GREATER && WINDOWS
            , PipeSecurity? pipeSecurity = null
#endif
            )
            : base(pipeName, bufferSize)
        {
#if NET6_0_OR_GREATER && WINDOWS
            _pipe = NamedPipeServerStreamAcl.Create(
                ipcName,
                PipeDirection.InOut,
                maxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                bufferSize,
                bufferSize,
                pipeSecurity);
#else
            _pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                maxAllowedServerInstances,
#if USE_MESSAGE_MODE
                PipeTransmissionMode.Message,
#else
                PipeTransmissionMode.Byte,
#endif
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
