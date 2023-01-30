/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A state for all entities that will automatically synchronize the
    /// its position and rotation.
    /// </summary>
    public class EntityState : SubsystemState
    {
        /// <summary>
        /// Contains the configuration for transform synchronization.
        /// </summary>
        public SyncTransformConfig synchronizedTransform;

        /// <summary>
        /// The current rotation of the entity.
        /// </summary>
        [HideInInspector] public Quaternion rotation;

        /// <summary>
        /// The current position of the entity.
        /// </summary>
        [HideInInspector] public Vector3 position;

        /// <summary>
        /// Write the entity state to the outgoing message.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="buffer"></param>
        /// <param name="isSpawning"></param>
        public override void Write(BaseEntity entity, NetOutgoingMessage buffer, bool isSpawning)
        {
            if (entity.IsStatic && !isSpawning)
            {
                buffer.Write(true);
            }
            else
            {
                buffer.Write(false);

                var rotationCompression = synchronizedTransform.rotationCompression;
                var positionCompression = synchronizedTransform.positionCompression;
                var positionAxes = synchronizedTransform.positionAxes;

                if (positionCompression.enabled)
                {
                    buffer.WriteCompressedVector3(position, positionCompression.minimum, positionCompression.maximum, positionCompression.numberOfBits, positionAxes);
                }
                else
                {
                    buffer.Write(position, positionAxes);
                }

                if (rotationCompression)
                {
                    buffer.WriteCompressedQuaternion(rotation);
                }
                else
                {
                    buffer.Write(rotation);
                }
            }

            base.Write(entity, buffer, isSpawning);
        }

        /// <summary>
        /// Read and process the entity state from the incoming message.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="buffer"></param>
        /// <param name="isSpawning"></param>
        public override void Read(BaseEntity entity, NetIncomingMessage buffer, bool isSpawning)
        {
            if (!buffer.ReadBoolean())
            {
                var rotationCompression = synchronizedTransform.rotationCompression;
                var positionCompression = synchronizedTransform.positionCompression;
                var positionAxes = synchronizedTransform.positionAxes;

                if (positionCompression.enabled)
                {
                    buffer.ReadCompressedVector3(ref position, positionCompression.minimum, positionCompression.maximum, positionCompression.numberOfBits, positionAxes);
                }
                else
                {
                    buffer.ReadVector3(ref position, positionAxes);
                }
                
                if (rotationCompression)
                {
                    rotation = buffer.ReadCompressedQuaternion();
                }
                else
                {
                    rotation = buffer.ReadQuaternion();
                }
            }

            base.Read(entity, buffer, isSpawning);
        }
    }
}
