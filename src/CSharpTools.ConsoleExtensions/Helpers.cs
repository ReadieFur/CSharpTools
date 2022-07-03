using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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
        internal static readonly string[] args = Environment.GetCommandLineArgs();

        private static object lockObject = new object();
        private static ConcurrentQueue<Task> tasksToSync = new ConcurrentQueue<Task>();

        private static bool inputLocked = false;
        /*private static object stdInLockObject = new object();
        private static CancellationTokenSource? stdInLockCancellationSource = null;
        private static Task? stdInLockTask = null;*/

        private static int consoleWidth = Console.WindowWidth;
        private static int consoleHeight = Console.WindowHeight;
        #endregion

        #region Public fields
        public static event Action<int, int, int, int>? internalConsoleResized; //Access priority.
        public static event Action<int, int, int, int>? consoleResized;
        #endregion

        #region Private methods
        static Helpers()
        {
            //Static loop.
            new Timer(
                (state) =>
                {
                    if (!Monitor.TryEnter(lockObject)) { return; }

                    while (tasksToSync.Count > 0) if (tasksToSync.TryDequeue(out Task? task) && task != null) task.RunSynchronously();

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

                    Monitor.Exit(lockObject);
                },
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(1) //1ms wait between loops. Ideally there would be none.
            );
        }
        #endregion

        #region Public methods
        public static int GetSafeConsoleWidth()
        {
            return (int)Math.Round(Console.WindowWidth * 0.75, 0);
        }

        public static Task<T> QueueTask<T>(Func<T> func)
        {
            Task<T> task = new Task<T>(func);
            tasksToSync.Enqueue(task);
            return task;
        }

        public static Task QueueAction(Action action)
        {
            Task task = new Task(action);
            tasksToSync.Enqueue(task);
            return task;
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
            catch { }
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
