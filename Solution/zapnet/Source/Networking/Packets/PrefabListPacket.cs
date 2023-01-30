/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;
using Lidgren.Network;

namespace zapnet
{
    /// <summary>
    /// Data representing a list of network prefabs.
    /// </summary>
    public struct PrefabListPacket
    {
        /// <summary>
        /// A table of prefabs by their name and unique identifier.
        /// </summary>
        public Dictionary<string, ushort> Prefabs { get; set; }

        public void Write(NetOutgoingMessage buffer)
        {
            buffer.Write((ushort)Prefabs.Count);

            foreach (var kv in Prefabs)
            {
                buffer.Write(kv.Key);
                buffer.Write(kv.Value);
            }
        }

        public static PrefabListPacket Read(NetIncomingMessage buffer)
        {
            var packet = new PrefabListPacket
            {
                Prefabs = new Dictionary<string, ushort>()
            };

            var prefabCount = buffer.ReadUInt16();

            for (var i = 0; i < prefabCount; i++)
            {
                var prefabName = buffer.ReadString();
                var prefabId = buffer.ReadUInt16();

                packet.Prefabs[prefabName] = prefabId;
            }

            return packet;
        }
    }
}
