/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

namespace zapnet
{
    /// <summary>
    /// An enum representing internally used network channels.
    /// </summary>
    internal enum NetChannel
    {
        /// <summary>
        /// Used to send data specifically for entity states.
        /// </summary>
        EntityStates,

        /// <summary>
        /// Used to send data specifically for player input.
        /// </summary>
        PlayerInput,

        /// <summary>
        /// Used to send data specifically for synchronized variables.
        /// </summary>
        SyncVars
    }
}