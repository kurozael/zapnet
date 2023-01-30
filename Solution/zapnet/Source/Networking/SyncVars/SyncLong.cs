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
    /// A 64 bit signed integer synchronized across the network.
    /// </summary>
    public class SyncLong : SyncVar<long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncLong"/> class.
        /// </summary>
        public SyncLong() : base(0, SyncTarget.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncLong"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncLong(long value, SyncTarget target = SyncTarget.All) : base(value, target)
        {
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public override void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            SetValue(buffer.ReadInt64(), changeSilently);
        }

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        public override void Write(NetOutgoingMessage buffer, bool dirtyOnly)
        {
            buffer.Write(Value);
        }
    }
}
