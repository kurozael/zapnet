/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;
using System;

namespace zapnet
{
    /// <summary>
    /// A base entity behaviour that all entities must inherit.
    /// </summary>
    [RequireComponent(typeof(NetworkPrefab))]
    [RequireComponent(typeof(EntityState))]
    public class BaseEntity : EntitySubsystem
    {
        public delegate bool ClosestEntityFilter<T>(T target) where T : BaseEntity;

        protected List<EntitySubsystem> _subsystems;
        protected List<BufferTransform> _positionBuffer;
        protected NetworkPrefab _networkPrefab;
        protected HashSet<Player> _scopePlayers;

        private Dictionary<Type, EntitySubsystem> _subsystemMap;

        /// <summary>
        /// Get the network prefab that this entity was created from.
        /// </summary>
        public NetworkPrefab NetworkPrefab
        {
            get
            {
                if (!_networkPrefab)
                {
                    _networkPrefab = GetComponent<NetworkPrefab>();
                }

                return _networkPrefab;
            }
        }

        /// <summary>
        /// Get the unique prefab name of this entity.
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// Get the unique prefab identifier of this entity.
        /// </summary>
        public ushort PrefabId { get; set; }

        /// <summary>
        /// Get the server tick this entity was spawned on.
        /// </summary>
        public uint SpawnTick { get; set; }

        /// <summary>
        /// Whether or not this entity is static (does not move.)
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Whether or not this entity is frozen. Frozen entities do not have state updates
        /// sent to clients.
        /// </summary>
        public bool IsFrozen { get; set; }

        /// <summary>
        /// Get the entity's unique identifier.
        /// </summary>
        public uint EntityId { get; set; }

        /// <summary>
        /// While unused by default internally, this can be used by your game to automatically scope entities
        /// that are within range from players.
        /// </summary>
        [Header("Entity Networking")]
        [Tooltip("Can be used to automatically scope the entity based on distance from the player.")]
        public float interestRadius = 10f;

        /// <summary>
        /// Whether or not the entity is always in scope. Entities that are not always in scope must be scoped
        /// manually for each player.
        /// </summary>
        public bool alwaysInScope = false;

        /// <summary>
        /// Whether or not automatic interpolation for this entity should be enabled. Disable this if you
        /// want to use your own method of interpolation.
        /// </summary>
        public bool enableInterpolation = true;

        /// <summary>
        /// How many ticks it takes to fully interpolate the position and rotation of this entity
        /// to the ones provided by the most recent state update.
        /// </summary>
        public uint interpolationTicks = 8;

        /// <summary>
        /// The minimum amount of scopes needed for this entity to tick.
        /// </summary>
        public int minimumScopesForTick = 0;

        /// <summary>
        /// The minimum amount of scopes needed for this entity to be visible.
        /// </summary>
        public int minimumScopesForVisible = 0;

        private List<BaseEntity> _entitiesInRange;

        /// <summary>
        /// Get a hashed set of players that this entity is scoped to.
        /// </summary>
        public HashSet<Player> ScopePlayers
        {
            get
            {
                return _scopePlayers;
            }
        }

        /// <summary>
        /// Get a subsystem by its unique identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subsystemId"></param>
        /// <returns></returns>
        public T GetSubsystem<T>(ushort subsystemId) where T : EntitySubsystem
        {
            if (subsystemId == 0)
            {
                return (this as T);
            }

            var index = subsystemId - 1;

            if (index > _subsystems.Count)
            {
                return null;
            }

            return (_subsystems[index] as T);
        }

        /// <summary>
        /// Get a subystem with the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSubsystem<T>() where T : EntitySubsystem
        {
            var subsystemType = typeof(T);

            if (_subsystemMap.TryGetValue(subsystemType, out var subsystem))
            {
                return (T)subsystem;
            }

            return null;
        }

        /// <summary>
        /// Set the position of this entity without interpolation.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;

            if (Zapnet.Network.IsClient)
            {
                for (var i = 0; i < _positionBuffer.Count; i++)
                {
                    var buffer = _positionBuffer[i];
                    buffer.position = position;
                    buffer.rotation = rotation;
                    _positionBuffer[i] = buffer;
                }

                OnTeleported();
            }
        }

        /// <summary>
        /// Set the scope of this entity for all currently connected players.
        /// </summary>
        /// <param name="inScope"></param>
        public void SetScopeAll(bool inScope)
        {
            Zapnet.Entity.SetScopeAll(this, inScope);
        }

        /// <summary>
        /// Set the scope of this entity for the provided player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="inScope"></param>
        public void SetScope(Player player, bool inScope)
        {
            Zapnet.Entity.SetScope(player, this, inScope);
        }

        /// <summary>
        /// Get the distance between this entity and another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public float DistanceTo(BaseEntity other)
        {
            return Vector3.Distance(transform.position, other.transform.position);
        }

        /// <summary>
        /// Get the distance between this entit and the provided position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float DistanceTo(Vector3 position)
        {
            return Vector3.Distance(transform.position, position);
        }

        /// <summary>
        /// Get all entities of the specific type within a range of this one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range"></param>
        /// <returns></returns>
        public List<BaseEntity> GetEntitiesInRange<T>(float range = Mathf.Infinity) where T : BaseEntity
        {
            _entitiesInRange.Clear();

            var entities = Zapnet.Entity.Entities.List;
            var position = transform.position;

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = (entities[i] as T);

                if (entity && entity != this)
                {
                    var distance = Vector3.Distance(entity.transform.position, position);

                    if (distance <= range)
                    {
                        _entitiesInRange.Add(entity);
                    }
                }
            }

            return _entitiesInRange;
        }

        /// <summary>
        /// Get the closest entity of the provided type within range of this entity. Optionally a filter
        /// callback can be supplied to determine which entities get selected as candiates.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public T GetClosestEntity<T>(float range = Mathf.Infinity, ClosestEntityFilter<T> filter = null) where T : BaseEntity
        {
            T closestEntity = null;
            var closestDistance = 0f;
            var entities = Zapnet.Entity.Entities.List;
            var position = transform.position;

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = (entities[i] as T);

                if (entity && entity != this && (filter == null || filter(entity)))
                {
                    var distance = Vector3.Distance(entity.transform.position, position);

                    if (distance <= range && (closestEntity == null || distance < closestDistance))
                    {
                        closestEntity = entity;
                        closestDistance = distance;
                    }
                }
            }

            return closestEntity;
        }

        /// <summary>
        /// Get whether another entity is in view of this one within a tolerance range between 0 and 1.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool IsInView(BaseEntity other, float tolerance)
        {
            var direction = (other.transform.position - transform.position).normalized;
            var dot = Vector3.Dot(direction, transform.forward);
            return (dot >= 1f - tolerance);
        }

        /// <summary>
        /// Invoked when this entity's scope count has changed.
        /// </summary>
        /// <param name="count"></param>
        public virtual void OnScopeCountChanged(int count)
        {
            gameObject.SetActive(count >= minimumScopesForVisible);
        }

        /// <summary>
        /// Get whether or not this entity should tick.
        /// </summary>
        /// <returns></returns>
        public virtual bool ShouldTick()
        {
            if (Zapnet.Network.IsServer)
            {
                return (ScopePlayers.Count >= minimumScopesForTick);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get whether this entity is scoped for the provided player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual bool IsScoped(Player player)
        {
            return Zapnet.Entity.IsScoped(player, this);
        }

        /// <summary>
        /// Invoked when a remote call event is received.
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnRemoteCallEvent(RemoteCallEvent data)
        {
            var method = Zapnet.Network.GetRemoteCallMethod(data.MethodId);
            var target = GetSubsystem<EntitySubsystem>(data.SubsystemId);

            if (target && method != null)
            {
                if (method.GetParameters().Length == 2)
                {
                    method.Invoke(target, new object[] { data.Sender, data.Params });
                }
                else
                {
                    method.Invoke(target, new object[] { data.Params });
                }
            }
        }

        /// <summary>
        /// Invoked when the entity enters a player's scope.
        /// </summary>
        /// <param name="player">The player.</param>
        public override void OnPlayerScoped(Player player)
        {
            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnPlayerScoped(player);
            }

            base.OnPlayerScoped(player);
        }

        /// <summary>
        /// Invoked when the entity leaves a player's scope.
        /// </summary>
        /// <param name="player">The player.</param>
        public override void OnPlayerDescoped(Player player)
        {
            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnPlayerDescoped(player);
            }

            base.OnPlayerDescoped(player);
        }

        /// <summary>
        /// Fire an event for the provided unique packet identifier and data and
        /// events for all subsystems.
        /// </summary>
        /// <param name="packetId"></param>
        /// <param name="data"></param>
        public override void Call(byte packetId, INetworkPacket data)
        {
            base.Call(packetId, data);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].Call(packetId, data);
            }
        }

        /// <summary>
        /// Write all spawn data to the outgoing message.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="buffer"></param>
        public override void WriteSpawn(Player player, NetOutgoingMessage buffer)
        {
            WriteSyncVars(buffer, SyncTarget.All);

            base.WriteSpawn(player, buffer);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].WriteSpawn(player, buffer);
            }
        }

        /// <summary>
        /// Read all spawn data from the outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        public override void ReadSpawn(NetIncomingMessage buffer)
        {
            ReadSyncVars(buffer, SyncTarget.All, true);

            base.ReadSpawn(buffer);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].ReadSpawn(buffer);
            }
        }

        /// <summary>
        /// Process a state update for this entity from the server.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="serverTick"></param>
        public override void ProcessStateUpdate(NetIncomingMessage buffer, uint serverTick)
        {
            base.ProcessStateUpdate(buffer, serverTick);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].ProcessStateUpdate(buffer, serverTick);
            }
        }

        /// <summary>
        /// Add everything required for a state update for this entity.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="tick"></param>
        public override void AddStateUpdate(NetOutgoingMessage buffer, uint tick)
        {
            base.AddStateUpdate(buffer, tick);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].AddStateUpdate(buffer, tick);
            }
        }

        /// <summary>
        /// Write all state information here before its sent.
        /// </summary>
        /// <param name="isSpawning"></param>
        public override void WriteState(bool isSpawning)
        {
            var state = GetState<EntityState>();

            if (state)
            {
                state.position = transform.position;
                state.rotation = transform.rotation;
            }

            base.WriteState(isSpawning);
        }

        /// <summary>
        /// Process all state information here after it has been received.
        /// </summary>
        /// <param name="isSpawning"></param>
        public override void ReadState(bool isSpawning)
        {
            var state = GetState<EntityState>();

            if (state)
            {
                if (!isSpawning)
                {
                    var distance = Vector3.Distance(state.position, transform.position);

                    if (distance >= GetTeleportDistance())
                    {
                        Teleport(state.position, state.rotation);
                    }
                    else if (enableInterpolation)
                    {
                        var timestamp = UniversalTime.Milliseconds;

                        _positionBuffer.Add(new BufferTransform()
                        {
                            timestamp = timestamp,
                            position = state.position,
                            rotation = state.rotation
                        });
                    }
                }
                else
                {
                    transform.position = state.position;
                    transform.rotation = state.rotation;
                }
            }

            base.ReadState(isSpawning);
        }

        /// <summary>
        /// Invoked when the network ticks.
        /// </summary>
        public override void Tick()
        {
            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].Tick();
            }

            SendSynchronizeEvent();

            base.Tick();
        }

        /// <summary>
        /// Invoked when this entity is created.
        /// </summary>
        public override void OnCreated()
        {
            Subscribe<RemoteCallEvent>(OnRemoteCallEvent);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnCreated();
            }

            base.OnCreated();
        }

        /// <summary>
        /// Invoked when this entity is removed.
        /// </summary>
        public override void OnRemoved()
        {
            Unsubscribe<RemoteCallEvent>(OnRemoteCallEvent);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnRemoved();
            }

            base.OnRemoved();
        }

        /// <summary>
        /// Invoked when this entity has spawned.
        /// </summary>
        public override void OnSpawned()
        {
            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnSpawned();
            }

            base.OnSpawned();
        }

        /// <summary>
        /// Invoked when this entity has despawned.
        /// </summary>
        public override void OnDespawned()
        {
            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].OnDespawned();
            }

            base.OnDespawned();
        }

        /// <summary>
        /// Write all dirty synchronized variables to an outgoing message and all
        /// synchronized variables for subsystems.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        public override void WriteDirtySyncVars(NetOutgoingMessage buffer, SyncTarget target)
        {
            base.WriteDirtySyncVars(buffer, target);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].WriteDirtySyncVars(buffer, target);
            }
        }

        /// <summary>
        /// Write all synchronized variables to an outgoing message and all
        /// synchronized variables for subsystems.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        public override void WriteSyncVars(NetOutgoingMessage buffer, SyncTarget target)
        {
            base.WriteSyncVars(buffer, target);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].WriteSyncVars(buffer, target);
            }
        }

        /// <summary>
        /// Read and process all synchronized variables from an incoming message and all
        /// synchronized variables for subsystems.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="target"></param>
        /// <param name="changeSilently"></param>
        public override void ReadSyncVars(NetIncomingMessage buffer, SyncTarget target, bool changeSilently = false)
        {
            base.ReadSyncVars(buffer, target, changeSilently);

            var subsystemCount = _subsystems.Count;

            for (var i = 0; i < subsystemCount; i++)
            {
                _subsystems[i].ReadSyncVars(buffer, target, changeSilently);
            }
        }

        /// <summary>
        /// Get the total dirty synchronized variables count for the provided target
        /// including the count from all subsystems.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected int GetTotalDirtySyncVarCount(SyncTarget target)
        {
            var totalDirtyCount = 0;
            var subsystemCount = _subsystems.Count;

            if (_dirtySyncVars.TryGetValue(target, out var count))
            {
                totalDirtyCount += count;
            }

            for (var i = 0; i < subsystemCount; i++)
            {
                if (_subsystems[i].DirtySyncVars.TryGetValue(target, out count))
                {
                    totalDirtyCount += count;
                }
            }

            return totalDirtyCount;
        }

        /// <summary>
        /// Invoked when the entity is teleported.
        /// </summary>
        protected virtual void OnTeleported()
        {

        }

        /// <summary>
        /// Invoked when the synchronize event should be sent.
        /// </summary>
        protected virtual void SendSynchronizeEvent()
        {
            var totalDirtyCount = GetTotalDirtySyncVarCount(SyncTarget.All);

            if (totalDirtyCount > 0)
            {
                var evnt = CreateEvent<SynchronizeEvent>();

                evnt.SetDeliveryMethod(NetDeliveryMethod.ReliableSequenced);
                evnt.SetChannel(NetChannel.SyncVars);

                evnt.Data.Entity = Entity;
                evnt.Data.Target = SyncTarget.All;

                evnt.Send();
            }
        }

        /// <summary>
        /// Get the distance required for this entity to teleport to its new position
        /// instead of interpolating.
        /// </summary>
        /// <returns></returns>
        protected virtual float GetTeleportDistance()
        {
            return 1f;
        }

        protected virtual void Update()
        {
            var network = Zapnet.Network;

            if (network && network.IsClient)
            {
                if (enableInterpolation)
                {
                    InterpolateTransform();
                }
            }
        }

        protected virtual void Awake()
        {
            _entitiesInRange = new List<BaseEntity>();
            _positionBuffer = new List<BufferTransform>();
            _scopePlayers = new HashSet<Player>();
            _subsystemMap = new Dictionary<Type, EntitySubsystem>();
            _subsystems = new List<EntitySubsystem>();

            Initialize(this, 0);

            var subsystems = GetComponentsInChildren<EntitySubsystem>();

            for (var i = 0; i < subsystems.Length; i++)
            {
                var subsystem = subsystems[i];

                if (subsystem != this)
                {
                    var subsystemId = _subsystems.Count + 1;
                    var subsystemType = subsystem.GetType();

                    subsystem.Initialize(this, (ushort)subsystemId);

                    _subsystemMap[subsystemType] = subsystem;
                    _subsystems.Add(subsystem);
                }
            }
        }

        protected virtual void Start()
        {
            if (Zapnet.Network.IsServer)
            {
                OnScopeCountChanged(_scopePlayers.Count);
            }
        }

        /// <summary>
        /// Invoked when the position and rotation should be interpolated to the values
        /// in the latest state update.
        /// </summary>
        protected virtual void InterpolateTransform()
        {
            var currentTimeStamp = UniversalTime.Milliseconds;
            var renderTimeStamp = currentTimeStamp - ((1000 / NetSettings.TickRate) * interpolationTicks);
            var buffer = _positionBuffer;

            while (buffer.Count >= 2 && buffer[1].timestamp <= renderTimeStamp)
            {
                buffer.RemoveAt(0);
            }

            if (buffer.Count >= 2 && buffer[0].timestamp <= renderTimeStamp && renderTimeStamp <= buffer[1].timestamp)
            {
                var x0 = buffer[0].position;
                var x1 = buffer[1].position;
                var q0 = buffer[0].rotation;
                var q1 = buffer[1].rotation;
                var t0 = buffer[0].timestamp;
                var t1 = buffer[1].timestamp;

                transform.position = Vector3.Lerp(x0, x1, (float)(renderTimeStamp - t0) / (t1 - t0));
                transform.rotation = Quaternion.Lerp(q0, q1, (float)(renderTimeStamp - t0) / (t1 - t0));
            }
        }
    }
}
