//#define USE_MESSAGE_MODE

using System;
using System.IO;
using System.IO.Pipes;

namespace CSharpTools.Pipes
{
    public class PipeServer : APipe<NamedPipeServerStream>
    {
        protected override PipeStream _pipe { get; set; }

        public PipeServer(string pipeName, int bufferSize, int maxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances)
            : base(pipeName, bufferSize)
        {
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
