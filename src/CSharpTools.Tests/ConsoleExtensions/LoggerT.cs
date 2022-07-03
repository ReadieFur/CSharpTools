using CSharpTools.ConsoleExtensions;

namespace CSharpTools.Tests.ConsoleExtensions
{
    internal class LoggerT
    {
        public static void Main()
        {
            Logger.Trace("Trace.").Wait();
            Logger.Debug("Debug.").Wait();
            Logger.Info("Info.").Wait();
            Logger.Warning("Warning.").Wait();
            Logger.Error("Error.").Wait();
            Logger.Critical("Critical.").Wait();
        }
    }
}
