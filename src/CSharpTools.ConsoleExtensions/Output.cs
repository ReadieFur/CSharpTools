using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace CSharpTools.ConsoleExtensions
{
    public static class Output
    {
        #region Private fields
        private static ConcurrentDictionary<string, (int, object)> lineStore = new ConcurrentDictionary<string, (int, object)>();
        #endregion

        #region Private methods
        static Output()
        {
            Helpers.internalConsoleResized += OnConsoleResized;
        }

        private static void OnConsoleResized(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            foreach (string id in lineStore.Keys)
            {
                //Fix cursor reposition and page line wrapping on large resizes.
                if (oldHeight < newHeight) ClearLine(oldHeight - lineStore[id].Item1 - 1, false).Wait();
                else if (oldHeight > newHeight) ClearLine(newHeight - lineStore[id].Item1 - 1 - lineStore.Count, false).Wait();

                //If the new line is going to be shorter than the old line, clear the line to remove any trailing characters.
                if (FormatToWidth(lineStore[id].Item2).Length < FormatToWidth(lineStore[id].Item2, oldWidth).Length)
                {
                    int lineIndex = newHeight - lineStore[id].Item1 - 1;
                    if (lineIndex >= 0) ClearLine(lineIndex, false).Wait();
                }

                UpdateLine(id);
            }
        }

        private static void OrderLineStore()
        {
            lock (lineStore)
            {
                IOrderedEnumerable<KeyValuePair<string, (int, object)>>? lineStoreOrdered = lineStore.ToList().OrderBy(kv => kv.Value.Item1);
                int lastLineIndex = -1;
                foreach (KeyValuePair<string, (int, object)> pair in lineStoreOrdered)
                {
                    if (pair.Value.Item1 == ++lastLineIndex) continue;

                    Helpers.QueueAction(() =>
                    {
                        int lineIndex = Console.WindowHeight - pair.Value.Item1 - 1;
                        if (lineIndex < 0) { return; }
                        (int currentX, int currentY) = (Console.CursorLeft, Console.CursorTop);
                        ClearLine(lineIndex, false).Wait();
                        Console.SetCursorPosition(0, Console.WindowHeight - lastLineIndex - 1); //Move to the new line.
                        Console.Write(FormatToWidth(pair.Value.Item2)); //Write this object to the new line.

                        lineStore[pair.Key] = (lastLineIndex, pair.Value.Item2); //Store the new line index.

                        Console.SetCursorPosition(currentX, currentY);
                    }).Wait();
                }
            }
        }
        #endregion

        #region Public methods
        public static string FormatToWidth(object? obj, int? customWidth = null)
        {
            int width = customWidth ?? Helpers.GetSafeConsoleWidth();
            if (width < 0) return "";

            string message;

            string? objectStr = obj?.ToString();
            message = objectStr ?? "null";

            if (message.Length > width) //Allow some space to wrap around.
            { message = message.Substring(0, width - 3) + "..."; }

            //message += string.Concat(Enumerable.Repeat(' ', consoleWidth - message.Length));

            return message;
        }

        public static Task QueueWrite(object? message)
        {
            return Helpers.QueueAction(() => { Console.Write(message); });
        }

        public static Task QueueWriteLine(object? message)
        {
            return Helpers.QueueAction(() => { Console.WriteLine(message); });
        }

        /// <summary>
        /// NOTE: Only set 'queueTask' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task WriteAt(int fromLeft, int fromTop, object message, bool queueTask = true)
        {
            if (fromLeft > Console.WindowWidth - 1 || fromTop > Console.WindowHeight - 1) throw new IndexOutOfRangeException();

            Action action = () =>
            {
                (int currentX, int currentY) = (Console.CursorLeft, Console.CursorTop);
                Console.SetCursorPosition(fromLeft, fromTop);
                Console.Write(message);
                Console.SetCursorPosition(currentX, currentY);
            };

            return queueTask ? Helpers.QueueAction(action) : Task.Run(action);
        }

        /// <summary>
        /// NOTE: Only set 'queueTask' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task ClearLine(int lineIndex, bool queueTask = true)
        {
            if (lineIndex > Console.WindowWidth - 1) throw new IndexOutOfRangeException();

            Action action = () =>
            {
                (int currentX, int currentY) = (Console.CursorLeft, Console.CursorTop);
                Console.SetCursorPosition(0, lineIndex);
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                Console.SetCursorPosition(currentX, currentY);
            };

            return queueTask ? Helpers.QueueAction(action) : Task.Run(action);
        }

        public static string ReserveLine()
        {
            lock (lineStore)
            {
                string id;
                do id = new string(Enumerable.Repeat("0123456789", 5).Select(s => s[new Random().Next(s.Length)]).ToArray());
                while (lineStore.ContainsKey(id));
                //We can use the lineStore.Count as the line index as it should be in order
                //(it may not always be in order due to it being accessable by multiple threads so we call the order function here).
                OrderLineStore();
                lineStore.TryAdd(id, (lineStore.Count, null));
                return id;
            }
        }

        /// <summary>
        /// NOTE: Only set 'queueTask' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task UpdateLine(string id, object? newData = null, bool queueTask = true)
        {
            lock (lineStore)
            {
                if (!lineStore.TryGetValue(id, out (int, object) pair)) throw new KeyNotFoundException();

                if (newData != null) pair.Item2 = newData;

                Action action = () =>
                {
                    int lineIndex = Console.WindowHeight - pair.Item1 - 1;
                    if (lineIndex < 0) { return; }

                    (int currentX, int currentY) = (Console.CursorLeft, Console.CursorTop);
                    Console.SetCursorPosition(0, lineIndex);
                    Console.Write(FormatToWidth(pair.Item2));
                    Console.SetCursorPosition(currentX, currentY);
                };

                return queueTask ? Helpers.QueueAction(action) : Task.Run(action);
            }
        }

        /// <summary>
        /// NOTE: Only set 'queueTask' to 'false' IF you are running it from within a queued task.
        /// </summary>
        public static Task FreeLine(string id, bool queueTask = true)
        {
            lock (lineStore)
            {
                if (!lineStore.TryRemove(id, out (int, object) pair)) throw new KeyNotFoundException();

                Action action = () =>
                {
                    int lineIndex = Console.WindowHeight - pair.Item1 - 1;
                    if (lineIndex < 0) { return; }
                    ClearLine(lineIndex, false).Wait();
                    OrderLineStore();
                };

                return queueTask ? Helpers.QueueAction(action) : Task.Run(action);
            }
        }
        #endregion
    }
}
