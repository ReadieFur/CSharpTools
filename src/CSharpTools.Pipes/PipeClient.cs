//#define USE_MESSAGE_MODE

using System.IO.Pipes;
using System.Security.Principal;

namespace CSharpTools.Pipes
{
    public class PipeClient : APipe<NamedPipeClientStream>
    {
        protected override PipeStream _pipe { get; init; }

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
            pipe.ReadMode =
#if USE_MESSAGE_MODE
                PipeTransmissionMode.Message
#else
                PipeTransmissionMode.Byte
#endif
            ;

            base.OnConnectCallback();
        }
    }
}
