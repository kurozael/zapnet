/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// An event fired when a remote call is made.
    /// </summary>
    public class RemoteCallEvent : BaseEventData
    {
        /// <summary>
        /// The unique subsystem identifier for the remote call method.
        /// </summary>
        public ushort SubsystemId { get; set; }

        /// <summary>
        /// The unique network identifier for the remote call method.
        /// </summary>
        public byte MethodId { get; set; }

        /// <summary>
        /// Contains params as a net buffer to be read or write from.
        /// </summary>
        public NetBuffer Params { get; set; }

        public override void Write(NetOutgoingMessage buffer)
        {
            buffer.Write(SubsystemId);
            buffer.Write(MethodId);

            if (Params.LengthBytes > 0)
            {
                Params.ResetHead();

                var bytes = Params.ReadBytes(Params.LengthBytes);
                buffer.Write(true);
                buffer.Write(bytes.Length);
                buffer.Write(bytes);
            }
            else
            {
                buffer.Write(false);
            }
        }

        public override bool Read(NetIncomingMessage buffer)
        {
            SubsystemId = buffer.ReadUInt16();
            MethodId = buffer.ReadByte();

            if (buffer.ReadBoolean())
            {
                var paramSize = buffer.ReadInt32();

                if (paramSize > 0)
                {
                    var byteSize = buffer.ReadBytes(paramSize);

                    Params.Clear();
                    Params.Write(byteSize);
                }
            }

            return true;
        }

        public override void OnFetched()
        {
            if (Params == null)
            {
                Params = new NetBuffer();
            }

            Params.Clear();

            base.OnFetched();
        }
    }
}
