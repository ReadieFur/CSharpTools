using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CSharpTools.Pipes
{
    public static class Helpers
    {
        //Deprecated but I see no other solution Microsoft 🖕
        /// <summary>
        /// Serializes a struct to a byte array.
        /// </summary>
        public static byte[] Serialize<T>(T data) where T : struct
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            return stream.ToArray();
        }

        /// <summary>
        /// Deserializes a byte array to the specified struct.
        /// </summary>
        /// <param name="serializationBinder">Optional.</param>
        public static T Deserialize<T>(byte[] array, SerializationBinder? serializationBinder = null) where T : struct
        {
            var stream = new MemoryStream(array);
            var formatter = new BinaryFormatter();
            if (serializationBinder != null)
                formatter.Binder = serializationBinder;
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        }

        /// <summary>
        /// Computes the size of bytes required to store this struct as a byte array.
        /// <para>You must make sure your struct is marked with the <see cref="SerializableAttribute"/> attribute.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>The size of bytes required to store this struct as a byte array.</returns>
        /// <exception cref="ArgumentException">Throws this if T is not serializable.</exception>
        public static int ComputeBufferSizeOf<T>() where T : struct
        {
            if (!typeof(T).IsSerializable) throw new ArgumentException("T must be serializable.", nameof(T));
            return Serialize(new T()).Length;
        }
    }
}
