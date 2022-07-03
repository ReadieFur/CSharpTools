using System;
using System.Threading.Tasks;

namespace CSharpTools.ConsoleExtensions
{
    public static class Logger
    {
        #region Public fields
        public static ELogMode logMode =
#if DEBUG
            ELogMode.Debug
#else
            ELogMode.Info
#endif
        ;
        public static bool useColour = true;
        #endregion

        #region Private methods
        private static ConsoleColor GetConsoleColourForLogMode(ELogMode _logMode)
        {
            switch (_logMode)
            {
                case ELogMode.Debug:
                    return ConsoleColor.Blue;
                case ELogMode.Info:
                    return ConsoleColor.White;
                case ELogMode.Warning:
                    return ConsoleColor.Yellow;
                case ELogMode.Error:
                    return ConsoleColor.Red;
                case ELogMode.Critical:
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
        public static Task Log(ELogMode _logMode, object message, bool queue = true)
        {
            if (_logMode < logMode) return Task.FromResult(true);
            Action action = () =>
            {
                string prefix = $"[{Enum.GetName(typeof(ELogMode), _logMode)!.ToUpper()}]";

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
                else
                {
                    Console.WriteLine($"{prefix} {message}");
                }
            };
            return queue ? Helpers.QueueAction(action) : Task.Run(action);
        }

        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Debug(object message, bool queue = true) { return Log(ELogMode.Debug, message, queue); }
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Info(object message, bool queue = true) { return Log(ELogMode.Info, message, queue); }
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Warning(object message, bool queue = true) { return Log(ELogMode.Warning, message, queue); }
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Error(object message, bool queue = true) { return Log(ELogMode.Error, message, queue); }
        /// <summary>
        /// NOTE: Only set 'queue' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task Critical(object message, bool queue = true) { return Log(ELogMode.Critical, message, queue); }
        #endregion
    }
}
