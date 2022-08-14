using System.Runtime.Serialization.Formatters.Binary;

namespace CSharpTools.Pipes
{
    public static class Helpers
    {
        //Deprecated but I see no other solution Microsoft 🖕
        public static byte[] Serialize<T>(T data) where T : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            return stream.ToArray();
        }

        public static T Deserialize<T>(byte[] array) where T : struct
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }

        /// To make your struct serializable add the [System.Serializable] attribute above the type.
        ///// <returns>The size of bytes required to store this struct as a byte array OR -1 if the struct is not serializable.</returns>
        /// <returns>The size of bytes required to store this struct as a byte array.</returns>
        /// <exception cref="ArgumentException">Throws this if T is not serializable.</exception>
        public static int ComputeBufferSizeOf<T>() where T : struct
        {
            //if (!typeof(T).IsSerializable) return -1;
            if (!typeof(T).IsSerializable) throw new ArgumentException("T must be serializable.", nameof(T));
            return Serialize(new T()).Length;
        }
    }
}
