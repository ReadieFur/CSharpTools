using CSharpTools.ConsoleExtensions;

namespace CSharpTools.Tests.ConsoleExtensions
{
    internal class LoggerT
    {
        public static async void Main()
        {
            await Logger.Trace("Trace.");
            await Logger.Debug("Debug.");
            await Logger.Info("Info.");
            await Logger.Warning("Warning.");
            await Logger.Error("Error.");
            await Logger.Critical("Critical.");
        }
    }
}
