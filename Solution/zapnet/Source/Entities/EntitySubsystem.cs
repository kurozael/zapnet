/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// The base behaviour class for entity subsystems. Entity subsystems are separate
    /// components that can exist alongside an entity, they can contain synchronized
    /// variables and remote calls.
    /// </summary>
    [RequireComponent(typeof(BaseEntity))]
    public class EntitySubsystem : MonoBehaviour
    {
        public delegate void RemoteCallServerMethod(Player player, NetBuffer buffer);
        public delegate void RemoteCallClientMethod(NetBuffer buffer);

        [Header("Subsystem Settings")]
        [SerializeField]
        protected SubsystemState _state;

        protected Dictionary<byte, List<EventListener>> _eventListeners;
        protected Dictionary<SyncTarget, List<ISyncVar>> _syncVars;
        protected Dictionary<SyncTarget, int> _dirtySyncVars;
        protected ushort _subsystemId;
        protected BaseEntity _entity;

        /// <summary>
        /// Get the entity that this subsystem belongs to.
        /// </summary>
        public BaseEntity Entity
        {
            get
            {
                return _entity;
            }
        }

        /// <summary>
        /// Get this subsystem's unique identifier.
        /// </summary>
        public ushort SubsystemId
        {
            get
            {
                return _subsystemId;
            }
        }

        /// <summary>
        /// Get a dictionary of the total count of dirty synchronized variables where
        /// the key is the synchronize target.
        /// </summary>
        public Dictionary<SyncTarget, int> DirtySyncVars
        {
            get
            {
                return _dirtySyncVars;
            }
        }

        /// <summary>
        /// Get the subsystem's state object.
        /// </summary>
        public SubsystemState State { get { return _state; } }

        /// <summary>
        /// Get the entity state cast as the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetState<T>() where T : EntityState
        {
            return (_state as T);
        }

        /// <summary>
        /// Get a remote client call object for the provided method and the recipient to call it on.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="recipient"></param>
        /// <returns></returns>
        public RemoteCall RemoteClientCall(RemoteCallClientMethod method, Player recipient)
        {
            return RemoteCall(method.Method, new List<Player> { recipient });
        }

        /// <summary>
        /// Get a remote client call object for the provided method and the recipients to call it on.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="recipients"></param>
        /// <returns></returns>
        public RemoteCall RemoteClientCall(RemoteCallClientMethod method, List<Player> recipients)
        {
            return RemoteCall(method.Method, recipients);
        }

        /// <summary>
        /// Get a remote client call object for the provided method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public RemoteCall RemoteClientCall(RemoteCallClientMethod method)
        {
            return RemoteCall(method.Method);
        }

        /// <summary>
        /// Get a remote server call object for the provided method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public RemoteCall RemoteServerCall(RemoteCallServerMethod method)
        {
            return RemoteCall(method.Method);
        }

        /// <summary>
        /// Create a new event (fetched from a pool) for this entity with the provided event data type. Optionally pass
        /// in existing data to use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public NetworkEvent<T> CreateEvent<T>(T data = null) where T : BaseEventData, new()
        {
            var evnt = Zapnet.Network.CreateEvent<T>(data);
            evnt.SetEntity(Entity);
            return evnt;
        }

        /// <summary>
        /// Subscribe to the provided event data type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void Subscribe<T>(OnProcessEvent<T> callback) where T : BaseEventData
        {
            var packetId = Zapnet.Network.GetPacketId<T>();

            if (!_eventListeners.TryGetValue(packetId, out var list))
            {
                list = _eventListeners[packetId] = new List<EventListener>();
            }

            var listener = new EventListener
            {
                original = callback,
                callback = (data) =>
                {
                    callback(data as T);
                }
            };

            list.Add(listener);
        }

        /// <summary>
        /// Unsubscribe from the provided event data type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void Unsubscribe<T>(OnProcessEvent<T> callback) where T : BaseEventData
        {
            var packetId = Zapnet.Network.GetPacketId<T>();

            if (_eventListeners.TryGetValue(packetId, out var listeners))
            {
                for (var i = 0; i < listeners.Count; i++)
                {
                    if (listeners[i].original == (object)callback)
                    {
                        listeners.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the network ticks.
        /// </summary>
        public virtual void Tick()
        {
            if (Zapnet.Network.IsServer)
            {
                foreach (var kv in _syncVars)
                {
                    var target = kv.Key;

                    _dirtySyncVars[target] = 0;

                    var syncVars = kv.Value;
                    var syncVarCount = _syncVars[target];

                    for (var i = 0; i < syncVars.Count; i++)
                    {
                        var field = syncVars[i];

                        if (field.IsDirty && !field.IsPaused)
                        {
                            _dirtySyncVars[target]++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a state update for this subsystem from the server.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="serverTick"></param>
        public virtual void ProcessStateUpdate(NetIncomingMessage buffer, uint serverTick)
        {
            if (_state && serverTick % (NetSettings.StatePriority * _state.SendPriority) == 0)
            {
                _state.Read(Entity, buffer, false);
                ReadState(false);
            }
        }

        /// <summary>
        /// Add everything required for a state update for this subsystem.
        /// </summary>
        /// <param name="buffer"></param>
        public virtual void AddStateUpdate(NetOutgoingMessage buffer, uint tick)
        {
            if (_state && tick % (NetSettings.StatePriority * _state.SendPriority) == 0)
            {
                WriteState(false);
                _state.Write(Entity, buffer, false);
            }
        }

        /// <summary>
        /// Write all dirty synchronized variables to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        public virtual void WriteDirtySyncVars(NetOutgoingMessage buffer, SyncTarget target)
        {
            if (_syncVars.TryGetValue(target, out var syncVars))
            {
                var syncVarCount = syncVars.Count;
                var dirtySyncVars = 0;

                for (var i = 0; i < syncVarCount; i++)
                {
                    var field = syncVars[i];

                    if (field.IsDirty && !field.IsPaused)
                    {
                        dirtySyncVars++;
                    }
                }

                buffer.Write((byte)dirtySyncVars);

                for (var i = 0; i < syncVarCount; i++)
                {
                    var field = syncVars[i];

                    if (field.IsDirty && !field.IsPaused)
                    {
                        buffer.Write((byte)i);

                        var metadata = field.ReadMetadata();

                        if (metadata.LengthBytes > 0)
                        {
                            buffer.Write(true);

                            var bytes = metadata.ReadBytes(metadata.LengthBytes);
                            buffer.Write(bytes.Length);
                            buffer.Write(bytes);

                            field.ClearMetadata();
                        }
                        else
                        {
                            buffer.Write(false);
                        }

                        field.Write(buffer, true);
                        field.IsDirty = false;
                    }
                }
            }
        }

        /// <summary>
        /// Write all synchronized variables to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        public virtual void WriteSyncVars(NetOutgoingMessage buffer, SyncTarget target)
        {
            if (_syncVars.TryGetValue(target, out var syncVars))
            {
                var syncVarCount = syncVars.Count;

                buffer.Write((byte)syncVarCount);

                for (var i = 0; i < syncVarCount; i++)
                {
                    buffer.Write((byte)i);
                    buffer.Write(false);

                    syncVars[i].Write(buffer);
                }
            }
        }

        /// <summary>
        /// Read and process all synchronized variables from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        /// <param name="changeSilently"></param>
        public virtual void ReadSyncVars(NetIncomingMessage buffer, SyncTarget target, bool changeSilently = false)
        {
            if (_syncVars.TryGetValue(target, out var syncVars))
            {
                var variableCount = buffer.ReadByte();

                if (variableCount == 0)
                {
                    return;
                }

                var syncVarCount = syncVars.Count;

                for (var i = 0; i < variableCount; i++)
                {
                    var fieldIndex = buffer.ReadByte();

                    if (syncVarCount > fieldIndex)
                    {
                        var field = syncVars[fieldIndex];

                        if (buffer.ReadBoolean())
                        {
                            var paramSize = buffer.ReadInt32();

                            if (paramSize > 0)
                            {
                                var metadataBytes = buffer.ReadBytes(paramSize);

                                field.ClearMetadata();
                                field.Metadata.Write(metadataBytes);
                            }
                        }

                        field.Read(buffer, changeSilently);
                    }
                }
            }
        }

        /// <summary>
        /// Fire an event for the provided unique packet identifier and data.
        /// </summary>
        /// <param name="packetId"></param>
        /// <param name="data"></param>
        public virtual void Call(byte packetId, INetworkPacket data)
        {
            if (_eventListeners.TryGetValue(packetId, out var list))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    list[i].callback(data);
                }
            }
        }

        /// <summary>
        /// Initialize this subsystem with the provided entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="subsystemId"></param>
        public virtual void Initialize(BaseEntity entity, ushort subsystemId)
        {
            _eventListeners = new Dictionary<byte, List<EventListener>>();
            _dirtySyncVars = new Dictionary<SyncTarget, int>();
            _syncVars = new Dictionary<SyncTarget, List<ISyncVar>>();

            var fieldFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfos = GetType().GetFields(fieldFlags);
            var propertyInfos = GetType().GetProperties(fieldFlags);
            var baseInterface = typeof(ISyncVar);
            var syncVarList = new List<ISyncVar>();

            foreach (var field in fieldInfos)
            {
                if (baseInterface.IsAssignableFrom(field.FieldType))
                {
                    var syncVar = (ISyncVar)field.GetValue(this);
                    syncVarList.Add(syncVar);
                }
            }

            foreach (var property in propertyInfos)
            {
                if (baseInterface.IsAssignableFrom(property.PropertyType))
                {
                    var syncVar = (ISyncVar)property.GetValue(this);
                    syncVarList.Add(syncVar);
                }
            }

            for (var i = 0; i < syncVarList.Count; i++)
            {
                var syncVar = syncVarList[i];
                var target = syncVar.Target;

                if (!_syncVars.ContainsKey(target))
                {
                    _syncVars[target] = new List<ISyncVar>();
                }

                if (!_dirtySyncVars.ContainsKey(target))
                {
                    _dirtySyncVars[target] = 0;
                }

                _syncVars[target].Add(syncVar);
            }

            _subsystemId = subsystemId;
            _entity = entity;
        }

        /// <summary>
        /// Write all spawn data to the outgoing message.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="buffer"></param>
        public virtual void WriteSpawn(Player player, NetOutgoingMessage buffer)
        {
            if (_state)
            {
                WriteState(true);
                _state.Write(Entity, buffer, true);
            }
        }

        /// <summary>
        /// Read all spawn data from the outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        public virtual void ReadSpawn(NetIncomingMessage buffer)
        {
            if (_state)
            {
                _state.Read(Entity, buffer, true);
                ReadState(true);
            }
        }

        /// <summary>
        /// Write all state information here before its sent.
        /// </summary>
        /// <param name="isSpawning"></param>
        public virtual void WriteState(bool isSpawning)
        {
            
        }

        /// <summary>
        /// Process all state information here after it has been received.
        /// </summary>
        /// <param name="isSpawning"></param>
        public virtual void ReadState(bool isSpawning)
        {
            
        }

        /// <summary>
        /// Invoked when the entity is created.
        /// </summary>
        public virtual void OnCreated()
        {
            
        }

        /// <summary>
        /// Invoked when the entity is removed.
        /// </summary>
        public virtual void OnRemoved()
        {
            
        }

        /// <summary>
        /// Invoked when the entity has spawned.
        /// </summary>
        public virtual void OnSpawned()
        {
            
        }

        /// <summary>
        /// Invoked when the entity has despawned.
        /// </summary>
        public virtual void OnDespawned()
        {
            
        }

        /// <summary>
        /// Invoked when the entity enters a player's scope.
        /// </summary>
        /// <param name="player">The player.</param>
        public virtual void OnPlayerScoped(Player player)
        {

        }

        /// <summary>
        /// Invoked when the entity leaves a player's scope.
        /// </summary>
        /// <param name="player">The player.</param>
        public virtual void OnPlayerDescoped(Player player)
        {

        }

        /// <summary>
        /// Invoked when the entity is teleported.
        /// </summary>
        public virtual void OnTeleported(Vector3 position, Quaternion rotation)
        {

        }

        private RemoteCall RemoteCall(MethodInfo method, List<Player> recipients = null)
        {
            var methodId = Zapnet.Network.GetRemoteCallId(method);
            var attribute = Zapnet.Network.GetRemoteCallAttribute(methodId);

            if (methodId > 0)
            {
                var evnt = CreateEvent<RemoteCallEvent>();

                evnt.SetEntity(Entity);

                if (recipients != null && recipients.Count > 0)
                {
                    evnt.SetRecipients(recipients);
                }

                evnt.Data.SubsystemId = _subsystemId;
                evnt.Data.MethodId = methodId;

                return new RemoteCall(evnt, attribute.InvokeOnSelf);
            }

            return default;
        }
    }
}
