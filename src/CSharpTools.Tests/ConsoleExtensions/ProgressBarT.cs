using ConsoleTools = CSharpTools.ConsoleExtensions;

namespace CSharpTools.Tests.ConsoleExtensions
{
    internal class ProgressBarT
    {
        public static void Main()
        {
            RunTests(true);
            RunTests(false);
        }

        private static void RunTests(bool useNewLine)
        {
            if (!useNewLine)
            {
                Console.WriteLine(
                    "This test will demonstrate the following:\n" +
                        "\tProgress bars\n" +
                        "\tPosition managment\n" +
                    "\nInfo:\n" +
                    "Auto bars will use the helper class to align\nitems from the bottom of the console.\n" +
                    "Manually positioned bars take a line index to write to,\nthese bars are not as safe when the console is resized.\n" +
                    "\nLogs:"
                );

                ConsoleTools.ProgressBar progressBar1 = new ConsoleTools.ProgressBar();
                ConsoleTools.ProgressBar progressBar2 = new ConsoleTools.ProgressBar();
                //Manually positioned bars can break when the UI is resized/it gets scrolled out of view, this will cause text on the target line to be overwritten.
                int positionedLineIndex = (int)Math.Round(Console.WindowHeight * 0.75, 0);
                ConsoleTools.ProgressBar progressBar3 = new ConsoleTools.ProgressBar(positionedLineIndex);
                Task.WaitAll(new Task[]
                {
                    Task.Run(() => RunProgressBar(progressBar1, "[PART 1] Auto Bar Short", 100, 50, false)).ContinueWith(t => progressBar1.SetPrefix("[WAITING] Auto Bar Short")),
                    //No point adding the other ContinueWith here as it will never finish before the first task.
                    Task.Run(() => RunProgressBar(progressBar2, "[PART 1] Auto Bar Looooooooooooooooooooooooooooooooooooooooooooooooong", 100, 100, false)),
                    Task.Run(() => RunProgressBar(progressBar3, "[PART 1] Positioned Bar", 100, 75, false)).ContinueWith(t => progressBar3.SetPrefix("[WAITING] Positioned Bar"))
                });
                Task.WaitAll(new Task[]
                {
                    Task.Run(() => RunProgressBar(progressBar1, "[PART 2] Auto Bar Short", 100, 50, true)).ContinueWith(t => Console.WriteLine("Auto Bar Short Complete.")),
                    Task.Run(() => RunProgressBar(progressBar2, "[PART 2] Auto Bar Looooooooooooooooooooooooooooooooooooooooooooooooong", 100, 100, true))
                        .ContinueWith(t => Console.WriteLine("Auto Bar Long Complete.")),
                    Task.Run(() => RunProgressBar(progressBar3, "[PART 2] Positioned Bar", 100, 75, true)).ContinueWith((t) =>
                    {
                        Console.WriteLine("Positioned Bar complete.");
                        //Because this bar was positioned manually it won't be automatically cleared.
                        ConsoleTools.Output.ClearLine(positionedLineIndex).Wait();
                    })
                });
            }
            else
            {
                Console.WriteLine(
                    "This test will demonstrate the following:\n" +
                        "\tProgress bars\n" +
                    "\nLogs:"
                );

                Task.Run(() =>
                {
                    RunProgressBar(
                        new ConsoleTools.ProgressBar(true, "Progress"),
                        "Progress",
                        10,
                        100,
                        true
                    );
                }).Wait();
            }
        }

        private static void RunProgressBar(ConsoleTools.ProgressBar progressBar, string prefix, int frequency, int interval, bool disposeWhenDone)
        {
            progressBar.SetPrefix(prefix);
            for (int i = 0; i <= frequency; i++)
            {
                progressBar.SetProgress(i * 100 / frequency); //Convert 0-frequency to 0-100.
                Thread.Sleep(interval);
            }
            if (disposeWhenDone) progressBar.Dispose();
        }
    }
}
