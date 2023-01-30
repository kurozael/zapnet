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
    /// A base event input data class that should be inherited to send input data to the server.
    /// </summary>
    public class BaseInputEvent : BaseEventData
    {
        /// <summary>
        /// The unique sequence number for this input event.
        /// </summary>
        public int SequenceNumber { get; set; }

        public override void Write(NetOutgoingMessage buffer)
        {
            buffer.Write(SequenceNumber);
        }

        public override bool Read(NetIncomingMessage buffer)
        {
            SequenceNumber = buffer.ReadInt32();

            return true;
        }

        public override void Recycle() { }
    }
}
