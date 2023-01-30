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
    /// Data representing all currently connected players.
    /// </summary>
    public struct PlayerListPacket
    {
        /// <summary>
        /// A list of unique player identifiers.
        /// </summary>
        public List<uint> PlayerIds { get; set; }

        /// <summary>
        /// The local player's unique identifier.
        /// </summary>
        public uint LocalPlayerId { get; set; }

        public void Write(NetOutgoingMessage buffer)
        {
            var playerCount = PlayerIds.Count;

            buffer.Write(LocalPlayerId);
            buffer.Write(playerCount);
            
            for (var i = 0; i < playerCount; i++)
            {
                buffer.Write(PlayerIds[i]);
            }
        }

        public static PlayerListPacket Read(NetIncomingMessage buffer)
        {
            var packet = new PlayerListPacket
            {
                LocalPlayerId = buffer.ReadUInt32(),
                PlayerIds = new List<uint>()
            };

            var playerCount = buffer.ReadInt32();

            for (var i = 0; i < playerCount; i++)
            {
                packet.PlayerIds.Add(buffer.ReadUInt32());
            }

            return packet;
        }
    }
}
