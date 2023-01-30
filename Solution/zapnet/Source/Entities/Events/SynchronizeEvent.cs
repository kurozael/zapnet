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
    /// An event fired when synchronized variables are changed.
    /// </summary>
    public class SynchronizeEvent : BaseEventData
    {
        /// <summary>
        /// The target type of players to synchronize with.
        /// </summary>
        public SyncTarget Target { get; set; }

        public override EarlyEventSettings GetEarlyEventSettings()
        {
            return new EarlyEventSettings
            {
                shouldWait = true,
                onlyLatest = true
            };
        }

        public override void Write(NetOutgoingMessage buffer)
        {
            buffer.Write((byte)Target);
            Entity.WriteDirtySyncVars(buffer, Target);
        }

        public override bool Read(NetIncomingMessage buffer)
        {
            Target = (SyncTarget)buffer.ReadByte();
            Entity.ReadSyncVars(buffer, Target);

            return true;
        }
    }
}
