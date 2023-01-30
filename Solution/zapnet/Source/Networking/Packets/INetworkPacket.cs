/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;

namespace zapnet
{
    /// <summary>
    /// An interfacing representing a poolable network packet.
    /// </summary>
    public interface INetworkPacket : INetworkPoolable
    {
        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        bool Read(NetIncomingMessage buffer);

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        void Write(NetOutgoingMessage buffer);
    }
}
