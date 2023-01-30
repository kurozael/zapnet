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
    /// An event fired when a player loses control of an entity.
    /// </summary>
    public class ControlLostEvent : BaseEventData
    {
        /// <summary>
        /// The controllable entity.
        /// </summary>
        public BaseEntity Controllable { get; set; }

        /// <summary>
        /// The controlling player.
        /// </summary>
        public Player Controller { get; set; }

        public override void Write(NetOutgoingMessage buffer)
        {
            buffer.Write(Controllable.EntityId);
            buffer.Write(Controller.PlayerId);
        }

        public override bool Read(NetIncomingMessage buffer)
        {
            Controllable = Zapnet.Entity.Find(buffer.ReadUInt32());
            Controller = Zapnet.Player.Find(buffer.ReadUInt16());

            return true;
        }
    }
}
