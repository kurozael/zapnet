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
    /// A base subsystem state class that all subsystem states must inherit. States are automatically
    /// sent to each client periodically.
    /// </summary>
    public class SubsystemState : MonoBehaviour
    {
        /// <summary>
        /// The rate at which this state will be sent to clients. A value of 1 means this state will be included in every update, a value of 2 means
        /// this state will be included in every 2nd update, and so on.
        /// </summary>
        [Tooltip("A value of 1 means this state will be included in every update, a value of 2 means this state will be included in every 2nd update, and so on.")]
        [SerializeField] protected int _sendPriority = 1;

        /// <summary>
        /// The rate at which this state will be sent to clients. A value of 1 means this state will be included in every update, a value of 2 means
        /// this state will be included in every 2nd update, and so on.
        /// </summary>
        public int SendPriority
        {
            get
            {
                return _sendPriority;
            }
            set
            {
                _sendPriority = value;
            }
        }

        /// <summary>
        /// Write the subsystem state to the outgoing message.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="buffer"></param>
        /// <param name="isSpawning"></param>
        public virtual void Write(BaseEntity entity, NetOutgoingMessage buffer, bool isSpawning)
        {
            
        }

        /// <summary>
        /// Read and process the subsystem state from the incoming message.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="buffer"></param>
        /// <param name="isSpawning"></param>
        public virtual void Read(BaseEntity entity, NetIncomingMessage buffer, bool isSpawning)
        {
            
        }
    }
}
