/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Unity.Collections.LowLevel.Unsafe;
using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A static class containing extensions for Lidgren.
    /// </summary>
    public static class LidgrenExtensions
    {
        private const float _precisionMultiplier = 32767f;

        /// <summary>
        /// Reset the head of the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public static void ResetHead(this NetBuffer buffer)
        {
            buffer.Position = 0;
        }

        /// <summary>
        /// Clear the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public static void Clear(this NetBuffer buffer)
        {
            buffer.LengthBytes = 0;
            buffer.Position = 0;
        }

        /// <summary>
        /// Write a Vector3 to the message with only the supplied axes.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="vector"></param>
        /// <param name="axes"></param>
        public static void Write(this NetOutgoingMessage buffer, Vector3 vector, VectorAxes axes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z)
        {
            if (vector.magnitude == 0f)
            {
                buffer.Write(true);
            }
            else
            {
                buffer.Write(false);

                var flags = UnsafeUtility.EnumToInt(axes);

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    buffer.Write(vector.x);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    buffer.Write(vector.y);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    buffer.Write(vector.z);
                }
            }
        }

        /// <summary>
        /// Write a compressed Vector3 to the message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="vector"></param>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <param name="numberOfBits"></param>
        /// <param name="axes"></param>
        public static void WriteCompressedVector3(this NetOutgoingMessage buffer, Vector3 vector, float minimum = -1024f, float maximum = 1024f, int numberOfBits = 16, VectorAxes axes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z)
        {
            if (vector.magnitude == 0f)
            {
                buffer.Write(true);
            }
            else
            {
                buffer.Write(false);

                var flags = UnsafeUtility.EnumToInt(axes);

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    buffer.WriteRangedSingle(vector.x, minimum, maximum, numberOfBits);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    buffer.WriteRangedSingle(vector.y, minimum, maximum, numberOfBits);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    buffer.WriteRangedSingle(vector.z, minimum, maximum, numberOfBits);
                }
            }
        }
        
        /// <summary>
        /// Write a compressed Quaternion to the message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="rotation"></param>
        public static void WriteCompressedQuaternion(this NetOutgoingMessage buffer, Quaternion rotation)
        {
            var maxIndex = (byte)0;
            var maxValue = float.MinValue;
            var sign = 1f;

            for (int i = 0; i < 4; i++)
            {
                var element = rotation[i];
                var abs = Mathf.Abs(rotation[i]);

                if (abs > maxValue)
                {
                    sign = (element < 0) ? -1 : 1;
                    maxIndex = (byte)i;
                    maxValue = abs;
                }
            }

            if (Mathf.Approximately(maxValue, 1f))
            {
                buffer.Write((byte)(maxIndex + 4));
                return;
            }

            var a = (short)0;
            var b = (short)0;
            var c = (short)0;

            if (maxIndex == 0)
            {
                a = (short)Mathf.RoundToInt(rotation.y * sign * _precisionMultiplier);
                b = (short)Mathf.RoundToInt(rotation.z * sign * _precisionMultiplier);
                c = (short)Mathf.RoundToInt(rotation.w * sign * _precisionMultiplier);
            }
            else if (maxIndex == 1)
            {
                a = (short)Mathf.RoundToInt(rotation.x * sign * _precisionMultiplier);
                b = (short)Mathf.RoundToInt(rotation.z * sign * _precisionMultiplier);
                c = (short)Mathf.RoundToInt(rotation.w * sign * _precisionMultiplier);
            }
            else if (maxIndex == 2)
            {
                a = (short)Mathf.RoundToInt(rotation.x * sign * _precisionMultiplier);
                b = (short)Mathf.RoundToInt(rotation.y * sign * _precisionMultiplier);
                c = (short)Mathf.RoundToInt(rotation.w * sign * _precisionMultiplier);
            }
            else
            {
                a = (short)Mathf.RoundToInt(rotation.x * sign * _precisionMultiplier);
                b = (short)Mathf.RoundToInt(rotation.y * sign * _precisionMultiplier);
                c = (short)Mathf.RoundToInt(rotation.z * sign * _precisionMultiplier);
            }

            buffer.Write(maxIndex);
            buffer.Write(a);
            buffer.Write(b);
            buffer.Write(c);
        }

        /// <summary>
        /// Read a compressed Vector3 from the message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="numberOfBits"></param>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static Vector3 ReadCompressedVector3(this NetIncomingMessage buffer, float min = -1024, float max = 1024, int numberOfBits = 16, VectorAxes axes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z)
        {
            var vector = Vector3.zero;
            buffer.ReadCompressedVector3(ref vector, min, max, numberOfBits, axes);
            return vector;
        }

        /// <summary>
        /// Read a Vector3 from the message into the referenced vector.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="vector"></param>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static void ReadVector3(this NetIncomingMessage buffer, ref Vector3 vector, VectorAxes axes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z)
        {
            var flags = UnsafeUtility.EnumToInt(axes);

            if (buffer.ReadBoolean())
            {
                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    vector.x = 0f;
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    vector.y = 0f;
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    vector.z = 0f;
                }
            }
            else
            {
                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    vector.x = buffer.ReadSingle();
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    vector.y = buffer.ReadSingle();
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    vector.z = buffer.ReadSingle();
                }
            }
        }

        /// <summary>
        /// Read a compressed Vector3 from the message into the referenced vector.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="vector"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="numberOfBits"></param>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static void ReadCompressedVector3(this NetIncomingMessage buffer, ref Vector3 vector, float min = -1024, float max = 1024, int numberOfBits = 16, VectorAxes axes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z)
        {
            var flags = UnsafeUtility.EnumToInt(axes);

            if (buffer.ReadBoolean())
            {
                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    vector.x = 0f;
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    vector.y = 0f;
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    vector.z = 0f;
                }
            }
            else
            {
                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.X)) != 0)
                {
                    vector.x = buffer.ReadRangedSingle(min, max, numberOfBits);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Y)) != 0)
                {
                    vector.y = buffer.ReadRangedSingle(min, max, numberOfBits);
                }

                if ((flags & UnsafeUtility.EnumToInt(VectorAxes.Z)) != 0)
                {
                    vector.z = buffer.ReadRangedSingle(min, max, numberOfBits);
                }
            }
        }

        /// <summary>
        /// Read a compressed Quaternion from the message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Quaternion ReadCompressedQuaternion(this NetIncomingMessage buffer)
        {
            var maxIndex = buffer.ReadByte();

            if (maxIndex >= 4 && maxIndex <= 7)
            {
                var x = (maxIndex == 4) ? 1f : 0f;
                var y = (maxIndex == 5) ? 1f : 0f;
                var z = (maxIndex == 6) ? 1f : 0f;
                var w = (maxIndex == 7) ? 1f : 0f;

                return new Quaternion(x, y, z, w);
            }

            var a = (float)buffer.ReadInt16() / _precisionMultiplier;
            var b = (float)buffer.ReadInt16() / _precisionMultiplier;
            var c = (float)buffer.ReadInt16() / _precisionMultiplier;
            var d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            if (maxIndex == 0)
                return new Quaternion(d, a, b, c);
            else if (maxIndex == 1)
                return new Quaternion(a, d, b, c);
            else if (maxIndex == 2)
                return new Quaternion(a, b, d, c);

            return new Quaternion(a, b, c, d);
        }
    }
}
