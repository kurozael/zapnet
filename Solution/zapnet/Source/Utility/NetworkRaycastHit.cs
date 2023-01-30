/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// Data representing a network raycast hit.
    /// </summary>
    public struct NetworkRaycastHit
    {
        /// <summary>
        /// The network hitbox object that was hit.
        /// </summary>
        public NetworkHitbox hitbox;

        /// <summary>
        /// The game object that was hit.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// The underlying Unity RaycastHit data.
        /// </summary>
        public RaycastHit data;
    }
}

