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
    /// An abstract class for all network event data.
    /// </summary>
    public abstract class BaseEventData : INetworkPacket
    {
        /// <summary>
        /// The entity this event is being invoked on.
        /// </summary>
        public BaseEntity Entity { get; set; }

        /// <summary>
        /// The player that sent this event.
        /// </summary>
        public Player Sender { get; set; }

        /// <summary>
        /// The tick number this event was sent on.
        /// </summary>
        public uint SendTick { get; set; }

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void Write(NetOutgoingMessage buffer);

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract bool Read(NetIncomingMessage buffer);

        /// <summary>
        /// Get the settings defining how the event should behave if it is received
        /// before an entity has spawned locally.
        /// </summary>
        /// <returns></returns>
        public virtual EarlyEventSettings GetEarlyEventSettings()
        {
            return default;
        }

        /// <summary>
        /// Called when the event should be returned to the event pool.
        /// </summary>
        public virtual void Recycle()
        {
            Zapnet.Network.Recycle(this);
        }

        /// <summary>
        /// When the object has been returned to its pool.
        /// </summary>
        public virtual void OnRecycled() {}

        /// <summary>
        /// When the object has been fetched from its pool.
        /// </summary>
        public virtual void OnFetched()
        {
            if (Zapnet.Network.IsListenServer)
            {
                Sender = Zapnet.Player.LocalPlayer;
            }
        }
    }
}
