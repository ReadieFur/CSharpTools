using CSharpTools.SharedMemory;

namespace CSharpTools.Tests.SharedMemory
{
    internal class SharedMemory
    {
        public static void Main()
        {
            string ipcName = "test_ipc";

            STestData testData;

            SharedMemory<STestData> sharedMemory1 = new SharedMemory<STestData>(ipcName);
            testData = new STestData { number = 1 };
            sharedMemory1.MutexWrite(testData);

            SharedMemory<STestData> sharedMemory2 = new SharedMemory<STestData>(ipcName);
            sharedMemory2.MutexRead(out testData);
            Console.WriteLine(testData.number); //1
            testData = new STestData { number = 2 };
            sharedMemory2.MutexWrite(testData);

            sharedMemory1.MutexRead(out testData);
            Console.WriteLine(testData.number); //2
        }
    }
}
