using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CSharpTools.ConsoleExtensions
{
    public static class Helpers
    {
        #region TODO
        /* TODO:
        * Override the input and error output.
        */
        #endregion

        #region Private fields
        public static readonly float safeConsoleWidthMultiplier = 0.75f;
        private static readonly object consoleLockObject = new object();
        private static readonly TimeSpan taskTimeout = TimeSpan.FromMilliseconds(1000);
        private static readonly ConcurrentQueue<Task> tasksToSync = new ConcurrentQueue<Task>();
        private static readonly Timer loop; //This requires a variable so that it remains referenced and does not get collected by the GC ending the loop.

        private static bool inputLocked = false;
        /*private static object stdInLockObject = new object();
        private static CancellationTokenSource? stdInLockCancellationSource = null;
        private static Task? stdInLockTask = null;*/

        private static int consoleWidth = Console.WindowWidth;
        private static int consoleHeight = Console.WindowHeight;
        #endregion

        #region Public fields
        public static readonly string[] commandLineArgs = Environment.GetCommandLineArgs();
        public static event Action<int, int, int, int>? internalConsoleResized; //Access priority.
        public static event Action<int, int, int, int>? consoleResized;
        #endregion

        #region Private methods
        static Helpers()
        {
            //Static loop.
            loop = new Timer((state) =>
            {
                if (Monitor.TryEnter(consoleLockObject, 0))
                {
                    while (tasksToSync.Count > 0) if (tasksToSync.TryDequeue(out Task task)) task.RunSynchronously();
                    Monitor.Exit(consoleLockObject);
                }

                /*Any code added to the taskQueue dosen't need to be waited here because:
                * - It will (most likley) be at the beginning of the queue.
                * - Will cause the program to halt becuase it will wait for the task to complete but the object is locked so it cannot run.
                */

                int _consoleWidth = Console.WindowWidth;
                int _consoleHeight = Console.WindowHeight;
                if (consoleWidth != _consoleWidth || consoleHeight != _consoleHeight)
                {
                    int oldWidth = consoleWidth;
                    int oldHeight = consoleHeight;
                    consoleWidth = _consoleWidth;
                    consoleHeight = _consoleHeight;

                    internalConsoleResized?.Invoke(oldWidth, oldHeight, consoleWidth, consoleHeight);
                    consoleResized?.Invoke(oldWidth, oldHeight, consoleWidth, consoleHeight);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1)); //1ms wait between loops. Ideally there would be none.
        }
        #endregion

        #region Internal methods
        internal static Task<bool> Run(Action<CancellationToken> action, bool queueTask = true)
        {
            if (queueTask) return QueueAction(action);
            else
            {
                action(new CancellationToken());
                return Task.FromResult(true);
            }
        }
        #endregion

        #region Public methods
        public static int GetSafeConsoleWidth() => (int)Math.Round(Console.WindowWidth * safeConsoleWidthMultiplier, 0);

        public static async Task<bool> QueueAction(Action<CancellationToken> action)
        {
            //My originaly plan was to somehow throw the task to stop execution of the task but this has proven to be MUCH harder than I thought.
            StackTrace stackTrace = new StackTrace();
            Task<bool> task = new Task<bool>(() =>
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                int firstCompletedTaskIndex = Task.WaitAny(Task.Delay(taskTimeout), Task.Run(() => action(cancellationTokenSource.Token)));
                if (firstCompletedTaskIndex == 0)
                {
                    Logger.Trace($"Queued task timed out after {taskTimeout.TotalMilliseconds}ms." +
                        " Continuing to the next queued task, console output conflicts may occur." +
                        //$" Stack trace at {stackTrace.GetFrame(3).GetMethod().ReflectedType.FullName}",
                        $" Stack trace at: {stackTrace.ToString().Substring(6)}",
                        false);

                    cancellationTokenSource.Cancel();

                    return false;
                }
                else return true;
            });
            tasksToSync.Enqueue(task);
            return await task;
        }

        /// <summary>
        /// Advised to not use, is thread blocking and buffered, once released the buffered input is dumped.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception"></exception>
        public static void LockInputHacky(CancellationToken cancellationToken)
        {
            if (inputLocked) { throw new Exception("stdIn already locked."); }
            inputLocked = true;
            try { Task.WaitAll(new Task[] { new Task(() => { while (true) { } }) }, cancellationToken); }
            catch {}
            inputLocked = false;
        }

        /*public static void LockStdIn(bool lockStdIn)
        {
            lock (stdInLockObject)
            {
                if (lockStdIn && stdInLockTask == null)
                {
                    stdInLockCancellationSource = new CancellationTokenSource();
                    stdInLockTask = new Task(() =>
                    {
                        Console.ReadKey(true);
                    }, stdInLockCancellationSource.Token);
                    stdInLockTask.Start();
                }
                else if (!lockStdIn && stdInLockTask != null)
                {
                    stdInLockCancellationSource!.Cancel();
                    stdInLockTask = null;
                }
            }
        }*/
        #endregion
    }
}
