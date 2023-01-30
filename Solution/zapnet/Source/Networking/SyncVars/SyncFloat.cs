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
    /// A 32 bit floating point value synchronized across the network.
    /// </summary>
    public class SyncFloat : SyncVar<float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncFloat"/> class.
        /// </summary>
        public SyncFloat() : base(0f, SyncTarget.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncFloat"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncFloat(float value, SyncTarget target = SyncTarget.All) : base(value, target)
        {
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public override void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            SetValue(buffer.ReadFloat(), changeSilently);
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
