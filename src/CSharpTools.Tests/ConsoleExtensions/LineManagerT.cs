using CSharpTools.ConsoleExtensions;

namespace CSharpTools.Tests.ConsoleExtensions
{
    internal class LineManagerT
    {
        public static void Main()
        {
            string message = "Hello, World!";
            if (message.Length > Console.WindowWidth) message = message.Substring(0, Console.WindowWidth);

            Output.WriteAt(
                    Console.WindowWidth - message.Length,
                    Console.WindowHeight - 1,
                    message
                )
                .ContinueWith(_ =>
                {
                    Console.WriteLine("Look at the bottom right of this window to see the positioned output.");
                    Thread.Sleep(2000);
                })
                .ContinueWith(_ => Output.ClearLine(Console.WindowHeight - 1))
                .Wait();
        }
    }
}
