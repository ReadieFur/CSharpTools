using CSharpTools.SharedMemory;

namespace CSharpTools.Tests.SharedMemory
{
    internal class SharedMemoryT
    {
        public static void Main()
        {
            string ipcName = "test_ipc";

            STestDataT testData;

            SharedMemory<STestDataT> sharedMemory1 = new SharedMemory<STestDataT>(ipcName);
            testData = new STestDataT { number = 1 };
            sharedMemory1.MutexWrite(testData);

            SharedMemory<STestDataT> sharedMemory2 = new SharedMemory<STestDataT>(ipcName);
            sharedMemory2.MutexRead(out testData);
            Console.WriteLine(testData.number); //1
            testData = new STestDataT { number = 2 };
            sharedMemory2.MutexWrite(testData);

            sharedMemory1.MutexRead(out testData);
            Console.WriteLine(testData.number); //2
        }
    }
}
