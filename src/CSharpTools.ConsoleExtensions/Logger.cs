using System;
using System.Threading.Tasks;

namespace CSharpTools.ConsoleExtensions
{
    public static class Logger
    {
        #region Public fields
        public static ELogLevel logLevel =
#if DEBUG
            ELogLevel.Debug
#else
            ELogLevel.Info
#endif
        ;
        public static bool useColour = true;
        #endregion

        #region Private methods
        private static ConsoleColor GetConsoleColourForLogMode(ELogLevel _logMode)
        {
            switch (_logMode)
            {
                case ELogLevel.Trace:
                    return ConsoleColor.Green;
                case ELogLevel.Debug:
                    return ConsoleColor.Blue;
                case ELogLevel.Info:
                    return ConsoleColor.White;
                case ELogLevel.Warning:
                    return ConsoleColor.Yellow;
                case ELogLevel.Error:
                    return ConsoleColor.Red;
                case ELogLevel.Critical:
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.Gray;
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Log(ELogLevel _logMode, object message, bool queue = true)
        {
            if (_logMode < logLevel) return Task.FromResult(true);
            return Helpers.Run((cancellationToken) =>
            {
                string prefix = $"[{Enum.GetName(typeof(ELogLevel), _logMode)!.ToUpper()}]";
                
                if (cancellationToken.IsCancellationRequested) return;
                
                if (useColour)
                {
                    ConsoleColor consoleColour = GetConsoleColourForLogMode(_logMode);
                    Console.BackgroundColor = consoleColour;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(prefix);
                    Console.BackgroundColor = default;
                    Console.ForegroundColor = consoleColour;
                    Console.WriteLine($" {message}");
                    Console.ResetColor();
                }
                else Console.WriteLine($"{prefix} {message}");
            }, queue);
        }

        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Trace(object message, bool queue = true) => Log(ELogLevel.Trace, message, queue);
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Debug(object message, bool queue = true) => Log(ELogLevel.Debug, message, queue);
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Info(object message, bool queue = true) => Log(ELogLevel.Info, message, queue);
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Warning(object message, bool queue = true) => Log(ELogLevel.Warning, message, queue);
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Error(object message, bool queue = true) => Log(ELogLevel.Error, message, queue);
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Critical(object message, bool queue = true) => Log(ELogLevel.Critical, message, queue);
        #endregion
    }
}
