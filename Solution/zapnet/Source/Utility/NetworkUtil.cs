/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace zapnet
{
    /// <summary>
    /// A static class of utilities relating to networking.
    /// </summary>
    public static class NetworkUtil
    {
        /// <summary>
        /// Serialize the provided input object into a byte array.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Serialize(this object input)
        {
            byte[] bytes;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, input);
                bytes = stream.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Deserialize the provided byte array into a type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this byte[] input)
        {
            T output;

            using (var stream = new MemoryStream(input))
            {
                var formatter = new BinaryFormatter();
                output = (T)formatter.Deserialize(stream);
            }

            return output;
        }
    }
}
