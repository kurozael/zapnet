/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

namespace zapnet
{
    /// <summary>
    /// Contains internally used message types for interfacing with Lidgren.
    /// </summary>
    internal enum MessageType
    {
        /// <summary>
        /// When a player has disconnected.
        /// </summary>
        PlayerDisconnected,

        /// <summary>
        /// When a player has connected.
        /// </summary>
        PlayerConnected,

        /// <summary>
        /// Change whether entities should be received.
        /// </summary>
        PreventSpawning,

        /// <summary>
        /// When initial data is received.
        /// </summary>
        InitialData,

        /// <summary>
        /// When an entity has spawned.
        /// </summary>
        SpawnEntity,

        /// <summary>
        /// When an entity has despawned.
        /// </summary>
        DespawnEntity,

        /// <summary>
        /// When  entities receive a state update.
        /// </summary>
        StateUpdate,

        /// <summary>
        /// When a network event is received.
        /// </summary>
        Event
    }
}
