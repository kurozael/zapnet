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
    /// A behaviour to manage all network players.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public delegate void OnPlayerEntityChanged(Player player, BaseEntity entity);
        public delegate void OnLocalPlayerReady();
        public delegate void OnPlayerAdded(Player player);
        public delegate void OnPlayerRemoved(Player player);
        public delegate void OnInitialDataSent(Player player);
        public delegate void OnInitialDataReceived(Player player);

        /// <summary>
        /// Invoked when a player's entity has changed.
        /// </summary>
        public event OnPlayerEntityChanged onPlayerEntityChanged;

        /// <summary>
        /// Invoked when the local player is ready.
        /// </summary>
        public event OnLocalPlayerReady onLocalPlayerReady;

        /// <summary>
        /// Invoked when a player has been added.
        /// </summary>
        public event OnPlayerAdded onPlayerAdded;

        /// <summary>
        /// Invoked when a player has been removed.
        /// </summary>
        public event OnPlayerRemoved onPlayerRemoved;

        /// <summary>
        /// Invoked when initial data has been sent.
        /// </summary>
        public event OnInitialDataSent onInitialDataSent;

        /// <summary>
        /// Invoked when initial data has been received.
        /// </summary>
        public event OnInitialDataReceived onInitialDataReceived;

        /// <summary>
        /// Get the local player.
        /// </summary>
        public Player LocalPlayer { get; set; }

        /// <summary>
        /// Get the local player's unique player identifier.
        /// </summary>
        public uint LocalPlayerId { get; set; }

        private uint _nextFreeId = 0;

        /// <summary>
        /// Get a table list of currently connected players.
        /// </summary>
        public TableList<Player> Players { get; } = new TableList<Player>();

        /// <summary>
        /// Get the next free unique player identifier.
        /// </summary>
        /// <returns></returns>
        public uint GetFreeId()
        {
            return ++_nextFreeId;
        }

        /// <summary>
        /// Find a player with the provided Lidgren NetConnection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public Player Find(NetConnection connection)
        {
            return (connection.Tag as Player);
        }

        /// <summary>
        /// Find a player with the provided unique player identifier.
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public Player Find(uint playerId)
        {
            return Players.Find(playerId);
        }

        internal void InvokeInitialDataReceived(Player player)
        {
            onInitialDataReceived?.Invoke(player);
            player.HasInitialData = true;
        }

        internal void InvokeInitialDataSent(Player player)
        {
            onInitialDataSent?.Invoke(player);
        }

        internal void InvokePlayerEntityChanged(Player player, BaseEntity entity)
        {
            onPlayerEntityChanged?.Invoke(player, entity);
        }

        internal Player Add(NetConnection connection = null, INetworkPacket credentials = null)
        {
            return Add(GetFreeId(), connection, credentials);
        }

        internal Player Add(uint playerId, NetConnection connection = null, INetworkPacket credentials = null)
        {
            if (Players.Exists(playerId))
            {
                return null;
            }

            var player = new Player
            {
                PlayerId = playerId,
                Connection = connection,
                LoginCredentials = credentials
            };

            Players.Add(playerId, player);

            if (playerId == LocalPlayerId)
            {
                LocalPlayer = player;
                onLocalPlayerReady?.Invoke();
            }

            onPlayerAdded?.Invoke(player);

            if (connection != null)
            {
                connection.Tag = player;
            }

            return player;
        }

        internal void Remove(Player player)
        {
            if (Players.Remove(player.PlayerId))
            {
                onPlayerRemoved?.Invoke(player);
            }
        }
    }
}
