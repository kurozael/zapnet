/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System;

namespace zapnet
{
    /// <summary>
    /// A static class providing access to a synchronized timestamp.
    /// </summary>
    public static class UniversalTime
    {
        private static readonly DateTime _stamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Get the current milliseconds elapsed since 1970 based on the UTC timezone. This is useful
        /// for timestamps that need to be the same on both the client and the server.
        /// </summary>
        public static ulong Milliseconds { get; private set; }

        internal static void Update()
        {
            Milliseconds = (ulong)(DateTime.UtcNow - _stamp).TotalMilliseconds;
        }
    }
}
