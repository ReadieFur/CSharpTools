using System;
using System.Threading;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace CSharpTools.SharedMemory
{
    public class SharedMemory<T> : IDisposable where T : struct
    {
        private Mutex mutex;
        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor accessor;

        public SharedMemory(string ipcName, int mutexSetupMillisecondsTimeout = -1)
        {
            //https://stackoverflow.com/questions/45441293/c-sharp-mutex-always-stuck-on-waitone
            /*mutex = new Mutex(true, ipcName + "_mutex", out bool isInitialOwner);
            if (!isInitialOwner)
            {
                mutex.Dispose();
                mutex = new Mutex(false, ipcName + "_mutex");
            }
            mutex.WaitOne(mutexSetupMillisecondsTimeout);
            mutex.ReleaseMutex();*/
            mutex = new Mutex(false, ipcName + "_mutex");
            mutex.WaitOne();

            try { mappedFile = MemoryMappedFile.OpenExisting(ipcName + "_map"); }
            catch (FileNotFoundException) { mappedFile = MemoryMappedFile.CreateNew(ipcName + "_map", Marshal.SizeOf<T>()); }

            accessor = mappedFile.CreateViewAccessor();

            mutex.ReleaseMutex();
        }

        ~SharedMemory() => Dispose();

        public void Dispose()
        {
            mutex?.Dispose();
            accessor?.Dispose();
            mappedFile?.Dispose();
        }

        public bool LockMutex(int millisecondsTimeout = -1)
        {
            if (millisecondsTimeout <= -1) return mutex.WaitOne();
            else return mutex.WaitOne(millisecondsTimeout);
        }

        public void ReleaseMutex() => mutex.ReleaseMutex();

        public T ReadFromBuffer()
        {
            accessor.Read(0, out T value);
            return value;
        }

        public void WriteToBuffer(T value) => accessor.Write(0, ref value);

        public bool MutexRead(out T value, int millisecondsTimeout = -1)
        {
            value = default(T);
            if (!LockMutex(millisecondsTimeout)) return false;
            value = ReadFromBuffer();
            ReleaseMutex();
            return true;
        }

        public bool MutexWrite(T value, int millisecondsTimeout = -1)
        {
            if (!LockMutex(millisecondsTimeout)) return false;
            WriteToBuffer(value);
            ReleaseMutex();
            return true;
        }
    }
}
