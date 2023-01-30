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
    /// A string synchronized across the network.
    /// </summary>
    public class SyncString : SyncVar<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncString"/> class.
        /// </summary>
        public SyncString() : base(null, SyncTarget.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncString"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncString(string value, SyncTarget target = SyncTarget.All) : base(value, target)
        {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SyncString"/> to <see cref="System.Boolean"/>.
        /// </summary>
        public static implicit operator bool(SyncString foo)
        {
            return !string.IsNullOrEmpty(foo.Value);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(SyncString a, string b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(SyncString a, string b)
        {
            return ReferenceEquals(a.Value, b);
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public override void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            SetValue(buffer.ReadString(), changeSilently);
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
