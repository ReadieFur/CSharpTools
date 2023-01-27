using System.IO.Pipes;
using System.Security.Principal;

namespace CSharpTools.Pipes
{
    public class PipeClient : APipe<NamedPipeClientStream>
    {
        protected override PipeStream _pipe { get; set; }

        /// <summary>
        /// Creates a pipe client that connects to a pipe server with the given name.
        /// </summary>
        /// <param name="ipcName">The name of the pipe server to connect to.</param>
        /// <param name="bufferSize">The buffer size for messages.</param>
        public PipeClient(string ipcName, int bufferSize) : base(ipcName, bufferSize)
        {
            _pipe = new NamedPipeClientStream(
                ".",
                ipcName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous,
                TokenImpersonationLevel.Identification);

            pipe.ConnectAsync().ContinueWith(_ => OnConnectCallback());
        }

        protected override void OnConnectCallback()
        {
            //https://stackoverflow.com/questions/4514784/pipetransmissionmode-message-how-do-net-named-pipes-distinguish-between-messag
            pipe.ReadMode = PipeTransmissionMode.Byte;
            base.OnConnectCallback();
        }
    }
}
