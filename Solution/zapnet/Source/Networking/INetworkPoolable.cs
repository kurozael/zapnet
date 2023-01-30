/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

namespace zapnet
{
    /// <summary>
    /// An interface for poolable network objects.
    /// </summary>
    public interface INetworkPoolable
    {
        /// <summary>
        /// When the object has been returned to its pool.
        /// </summary>
        void OnRecycled();

        /// <summary>
        /// When the object has been fetched from its pool.
        /// </summary>
        void OnFetched();
    }
}
