# CSharpTools.SharedMemory
This library contains a helper class to make using shared memory and buffers in C# easier.  

## Installation:
Grab the latest build from the releases folder [here](./bin/Release/netstandard2.0/CSharpTools.SharedMemory.dll).  
Once you have done that, add the dll to your projects refrence files.

## Example:
### Client:
```cs
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
```