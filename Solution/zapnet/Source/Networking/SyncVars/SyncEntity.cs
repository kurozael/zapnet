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
    /// An entity synchronized across the network.
    /// </summary>
    public class SyncEntity<T> : SyncVar<T> where T : BaseEntity
    {
        /// <summary>
        /// The pending identifier while waiting for the entity to be created.
        /// </summary>
        public uint? PendingId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncEntity{T}"/> class.
        /// </summary>
        public SyncEntity() : base(null, SyncTarget.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncEntity{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncEntity(T value, SyncTarget target = SyncTarget.All) : base(value, target)
        {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SyncEntity{T}"/> to <see cref="System.Boolean"/>.
        /// </summary>
        public static implicit operator bool(SyncEntity<T> foo)
        {
            return foo.Value;
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public override void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            var entityId = buffer.ReadUInt32();

            if (entityId == 0)
            {
                SetValue(null, changeSilently);
                return;
            }

            var entity = (Zapnet.Entity.Find(entityId) as T);

            if (entity != null)
            {
                SetValue(entity, changeSilently);
                return;
            }

            if (!PendingId.HasValue)
            {
                Zapnet.Entity.onEntityCreated += OnEntityCreated;
            }

            PendingId = entityId;
        }

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        public override void Write(NetOutgoingMessage buffer, bool dirtyOnly)
        {
            buffer.Write(Value ? Value.EntityId : 0);
        }

        private void OnEntityCreated(BaseEntity entity)
        {
            if (entity.EntityId == PendingId.Value && entity is T)
            {
                Zapnet.Entity.onEntityCreated -= OnEntityCreated;
                PendingId = null;
                Value = (entity as T);
            }
        }
    }
}
