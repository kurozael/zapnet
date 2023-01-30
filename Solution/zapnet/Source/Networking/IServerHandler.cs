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
    /// A generic interface for a server handler. Your game must have an implementation of this.
    /// </summary>
    public interface IServerHandler
    {
        /// <summary>
        /// When initial data should be written to send to a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="buffer"></param>
        void WriteInitialData(Player player, NetOutgoingMessage buffer);

        /// <summary>
        /// Whether or not a player can authenticate with the server.
        /// </summary>
        /// <param name="credentials"></param>
        /// <returns></returns>
        bool CanPlayerAuth(INetworkPacket credentials);

        /// <summary>
        /// When a player has connected.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="credentials"></param>
        void OnPlayerConnected(Player player, INetworkPacket credentials);

        /// <summary>
        /// When a player has disconnected.
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerDisconnected(Player player);

        /// <summary>
        /// When a player has received initial data.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="credentials"></param>
        void OnInitialDataReceived(Player player, INetworkPacket credentials);
    }
}
