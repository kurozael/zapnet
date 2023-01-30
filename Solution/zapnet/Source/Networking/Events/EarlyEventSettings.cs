/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

namespace zapnet
{
    /// <summary>
    /// Data representing settings to define how an event should behave if it was received
    /// before an entity has spawned locally.
    /// </summary>
    public struct EarlyEventSettings
    {
        /// <summary>
        /// Whether or not the event should wait until the entity spawns.
        /// </summary>
        public bool shouldWait;

        /// <summary>
        /// When the entity spawns, only invoke the latest event of this type received.
        /// </summary>
        public bool onlyLatest;

        /// <summary>
        /// How long before this event becomes stale and is discarded in seconds.
        /// </summary>
        public float staleTime;

        /// <summary>
        /// The maximum amount of events of this type that should be held at one time.
        /// </summary>
        public uint bufferSize;
    }
}

