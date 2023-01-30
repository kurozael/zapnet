/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;
using Lidgren.Network;
using System;

namespace zapnet
{
    /// <summary>
    /// An object representing a single player connection.
    /// </summary>
    public class Player : IEquatable<Player>
    {
        /// <summary>
        /// [Server] A hashed set of entities scoped by this player. These are all of the entities
        /// that the player can see.
        /// </summary>
        public HashSet<BaseEntity> ScopeEntities { get; }

        /// <summary>
        /// [Server] Get the login credentials network packet that the player provided when they
        /// initially connected.
        /// </summary>
        public INetworkPacket LoginCredentials { get; set; }

        /// <summary>
        /// [Shared] A hashed set of entities controlled by this player.
        /// </summary>
        public HashSet<BaseEntity> Controllables { get; }

        /// <summary>
        /// [Server] Get the underlying NetConnection from Lidgren. This will be null if the server
        /// is also the client.
        /// </summary>
        public NetConnection Connection { get; set; }

        /// <summary>
        /// [Server] Whether or not the spawning of entities should be prevented for this player.
        /// </summary>
        public bool PreventEntitySpawning { get; set; }

        /// <summary>
        /// [Server] Whether or not the player has received their initial data.
        /// </summary>
        public bool HasInitialData { get; internal set; }

        /// <summary>
        /// [Shared] Get the unique identifier representing this player.
        /// </summary>
        public uint PlayerId { get; internal set; }

        /// <summary>
        /// [Shared] Get the network entity that belongs to this player.
        /// </summary>
        public BaseEntity Entity { get; private set; }

        /// <summary>
        /// [Shared] Set the network entity that belongs to this player.
        /// </summary>
        /// <param name="entity"></param>
        public void SetEntity(BaseEntity entity)
        {
            if (Entity != entity)
            {
                Entity = entity;
                Zapnet.Player.InvokePlayerEntityChanged(this, Entity);
            }
        }

        /// <summary>
        /// [Shared] Whether or not this player is the local player.
        /// </summary>
        public bool IsLocalPlayer
        {
            get
            {
                return (Zapnet.Player.LocalPlayer == this);
            }
        }

        /// <summary>
        /// [Server] Whether or not this player is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                var connection = Connection;
                return (connection != null && connection.Status == NetConnectionStatus.Connected);
            }
        }

        public bool Equals(Player other)
        {
            return (PlayerId == other.PlayerId);
        }

        public Player()
        {
            ScopeEntities = new HashSet<BaseEntity>();
            Controllables = new HashSet<BaseEntity>();
        }
    }
}