using CSharpTools.ConsoleExtensions;

namespace CSharpTools.Tests.ConsoleExtensions
{
    internal class Testing
    {
        private const uint max = uint.MaxValue;

        public static void Main()
        {
            MemoryTests();
            //TimeoutTests();
        }

        private static async void MemoryTests()
        {
            //Fast, peaked at around 20mb memory.
            //for (uint i = 0; i < max; i++) Console.WriteLine(i);

            //Considerably slower and VERY quickly climbed to gigabytes of memory with no signs of leveling out.
            /*I suspect this is slower becuase of the following reasons:
            * 1. It has more instructions per log, i.e. get console colours and set them.
            * 2. It has to wait for each log to complete, slowing down due to the mutex locking.
            */
            //for (uint i = 0; i < max; i++) Logger.Info(i);

            //Faster than the queued log however you get a broken UI and
            //it still seems to climb very quickly in memory useage however at a slightly slower rate.
            //for (uint i = 0; i < max; i++) Logger.Info(i, false);

            //Faster than the queued log, no memory climb.
            //I suspect the memory climb existed in the other tests then because tasks kept getting pushed onto the queue very quickly,
            //as the loop could continue to execute while logs needed to be processed.
            //Whereas this method waits for them to complete, however I am not sure why the memory kept climbing in the unqueued test.
            for (uint i = 0; i < max; i++) await Logger.Info(i);

            //After obtaining the results in the last test, I was able to edit the third test (Logger.Info(i, false)) to,
            //not climb in memory by running the action directly instead of through a task (I dont know why I didn't do that in the first place).
        }

        private static async void TimeoutTests()
        {
            Logger.logLevel = ELogLevel.Trace;

            await Helpers.QueueAction((cancellationToken) =>
            {
                Thread.Sleep(10);
                Console.WriteLine(1);
            });

            await Helpers.QueueAction((cancellationToken) =>
            {
                Thread.Sleep(5000);
                if (cancellationToken.IsCancellationRequested) return;
                Console.WriteLine(2);
            });
        }
    }
}
