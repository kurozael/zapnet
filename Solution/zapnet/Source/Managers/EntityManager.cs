/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A behaviour to manage all network entities.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        public delegate void OnEntityCreated(BaseEntity entity);
        public delegate void OnEntityRemoved(BaseEntity entity);

        /// <summary>
        /// Invoked when an entity has been created.
        /// </summary>
        public event OnEntityCreated onEntityCreated;

        /// <summary>
        /// Invoked when an entity has been removed.
        /// </summary>
        public event OnEntityRemoved onEntityRemoved;

        private List<NetworkRaycastHit> _raycastResults;
        private List<BaseEntity> _entitiesInRange;
        private List<NetworkHitbox> _hitboxes;
        private RaycastHit[] _raycastHits;
        private NetworkRaycastHit _raycastResult;
        private bool _preventSpawning;
        private uint _nextFreeId = 0;

        /// <summary>
        /// Get a table list of all entities that exist.
        /// </summary>
        public TableList<BaseEntity> Entities { get; } = new TableList<BaseEntity>();

        /// <summary>
        /// [Client] Whether or not the spawning of entities should be prevented. This should
        /// only be set on the client and can be used to temporarily hold off spawning
        /// of entities until the client is ready.
        /// </summary>
        public bool PreventSpawning
        {
            get
            {
                return _preventSpawning;
            }
            set
            {
                if (_preventSpawning != value)
                {
                    _preventSpawning = value;

                    if (Zapnet.Player.LocalPlayer != null)
                    {
                        var message = Zapnet.Network.CreateMessage();

                        message.Write((byte)MessageType.PreventSpawning);
                        message.Write(_preventSpawning);

                        Zapnet.Network.SendToServer(message, NetDeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        /// <summary>
        /// [Shared] Add a network hitbox to be rewound when performing raycasts.
        /// </summary>
        /// <param name="hitbox"></param>
        public void AddHitbox(NetworkHitbox hitbox)
        {
            _hitboxes.Add(hitbox);
        }

        /// <summary>
        /// [Shared] Remove a network hitbox from the managed list.
        /// </summary>
        /// <param name="hitbox"></param>
        public void RemoveHitbox(NetworkHitbox hitbox)
        {
            _hitboxes.Remove(hitbox);
        }

        /// <summary>
        /// [Shared] Find an entity prefab with the provided unique name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public BaseEntity FindPrefab(string name)
        {
            return FindPrefab<BaseEntity>(name);
        }

        /// <summary>
        /// [Shared] Find an entity prefab with the provided unique name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T FindPrefab<T>(string name) where T : BaseEntity
        {
            return Zapnet.Prefab.Find<T>(name);
        }

        /// <summary>
        /// [Server] Get the next free unique entity identifier.
        /// </summary>
        /// <returns></returns>
        public uint GetFreeId()
        {
            return ++_nextFreeId;
        }

        /// <summary>
        /// [Shared] Remove the provided entity and despawn it on all clients.
        /// </summary>
        /// <param name="entityId"></param>
        public void Remove(uint entityId)
        {
            var entity = Find(entityId);

            if (entity)
            {
                Remove(entity);
            }
        }

        /// <summary>
        /// [Shared] Remove the provided entity and despawn it on all clients.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(BaseEntity entity)
        {
            if (Entities.List.Contains(entity))
            {
                Entities.Remove(entity.EntityId);

                if (Zapnet.Network.IsServer)
                {
                    var playerList = Zapnet.Player.Players.List;

                    for (var i = 0; i < playerList.Count; i++)
                    {
                        var player = playerList[i];
                        var scopeList = player.ScopeEntities;

                        if (scopeList.Contains(entity))
                        {
                            Despawn(player, entity);
                        }
                    }
                }

                entity.OnRemoved();

                onEntityRemoved?.Invoke(entity);

                Destroy(entity.gameObject);
            }
        }

        /// <summary>
        /// [Shared] Removes all entities from the scene. Calling this client-side will only
        /// remove the entities for the local player.
        /// </summary>
        public void RemoveAll()
        {
            var entities = Entities.List;

            for (var i = entities.Count - 1; i >= 0; i--)
            {
                Remove(entities[i]);
            }
        }

        /// <summary>
        /// Manually send an entity spawn message to the specified player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="entity">The entity.</param>
        internal void Send(Player player, BaseEntity entity)
        {
            if (player.IsConnected)
            {
                var message = Zapnet.Network.CreateMessage();

                message.Write((byte)MessageType.SpawnEntity);
                message.Write(Zapnet.Network.ServerTick);
                message.Write(entity.PrefabId);
                message.Write(entity.EntityId);

                entity.WriteSpawn(player, message);

                Zapnet.Network.SendTo(message, player.Connection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        internal BaseEntity Create(string prefabName, uint entityId)
        {
            return Create<BaseEntity>(prefabName, entityId);
        }

        internal void PreRaycastAll(uint tick)
        {
            _raycastResults.Clear();

            for (var i = 0; i < _hitboxes.Count; i++)
            {
                var hitbox = _hitboxes[i];
                hitbox.Backup();
                hitbox.Rewind(tick);
            }

            for (var i = 0; i < _raycastHits.Length; i++)
            {
                _raycastHits[i] = default;
            }
        }

        internal void PostRaycastAll()
        {
            for (var i = 0; i < _hitboxes.Count; i++)
            {
                var hitbox = _hitboxes[i];
                hitbox.Restore();
            }

            for (var i = 0; i < _raycastHits.Length; i++)
            {
                var hit = _raycastHits[i];

                if (hit.collider != null)
                {
                    var gameObject = hit.transform.gameObject;
                    var hitbox = gameObject.GetComponent<NetworkHitbox>();

                    _raycastResults.Add(new NetworkRaycastHit
                    {
                        gameObject = gameObject,
                        hitbox = hitbox,
                        data = hit
                    });
                }
            }
        }

        /// <summary>
        /// [Shared] Perform a spherecast against all objects and rewind all network hitboxes to the provided server tick.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="tick"></param>
        /// <param name="radius"></param>
        /// <param name="distance"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public List<NetworkRaycastHit> SpherecastAll(Ray ray, uint tick, float radius, float distance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            PreRaycastAll(tick);

            Physics.SphereCastNonAlloc(ray, radius, _raycastHits, distance, layerMask, QueryTriggerInteraction.Collide);

            PostRaycastAll();

            return _raycastResults;
        }

        /// <summary>
        /// [Shared] Perform a raycast against all objects and rewind all network hitboxes to the provided server tick.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="tick"></param>
        /// <param name="distance"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public List<NetworkRaycastHit> RaycastAll(Ray ray, uint tick, float distance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            PreRaycastAll(tick);

            Physics.RaycastNonAlloc(ray, _raycastHits, distance, layerMask, QueryTriggerInteraction.Collide);

            PostRaycastAll();

            return _raycastResults;
        }

        /// <summary>
        /// [Shared] Get all entities in range of the provided position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public List<BaseEntity> GetEntitiesInRange<T>(Vector3 position, float range = Mathf.Infinity) where T : BaseEntity
        {
            _entitiesInRange.Clear();

            var entities = Zapnet.Entity.Entities.List;

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
        /// [Shared] Perform a raycast and rewind network hitboxes to the provided server tick.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="tick"></param>
        /// <param name="distance"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public NetworkRaycastHit Raycast(Ray ray, uint tick, float distance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            for (var i = 0; i < _hitboxes.Count; i++)
            {
                var hitbox = _hitboxes[i];
                hitbox.Backup();
                hitbox.Rewind(tick);
            }

            Physics.Raycast(ray, out RaycastHit hitInfo, distance, layerMask, QueryTriggerInteraction.Collide);

            for (var i = 0; i < _hitboxes.Count; i++)
            {
                var hitbox = _hitboxes[i];
                hitbox.Restore();
            }

            if (hitInfo.collider != null)
            {
                var gameObject = hitInfo.transform.gameObject;
                var hitbox = gameObject.GetComponent<NetworkHitbox>();

                _raycastResult.gameObject = gameObject;
                _raycastResult.hitbox = hitbox;
                _raycastResult.data = hitInfo;
            }
            else
            {
                _raycastResult.gameObject = null;
                _raycastResult.hitbox = null;
            }

            return _raycastResult;
        }

        internal T Create<T>(string prefabName, uint entityId) where T : BaseEntity
        {
            if (Entities.Exists(entityId))
            {
                return null;
            }

            var prefab = FindPrefab<T>(prefabName);

            if (!prefab)
            {
                return null;
            }

            var entity = Instantiate(prefab);

            entity.transform.position = new Vector3(0f, 0.1f, 0f);
            entity.PrefabName = prefabName;
            entity.SpawnTick = Zapnet.Network.ServerTick;
            entity.PrefabId = Zapnet.Prefab.GetId(prefabName);
            entity.EntityId = entityId;

            return (T)Add(entity);
        }

        /// <summary>
        /// [Server] Create an entity from the provided network prefab name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public T Create<T>(string prefabName) where T : BaseEntity
        {
            return Create<T>(prefabName, GetFreeId());
        }

        internal T Create<T>(T prefab, uint entityId) where T : BaseEntity
        {
            var networkPrefab = prefab.NetworkPrefab;

            if (!networkPrefab)
            {
                return null;
            }

            return Create<T>(networkPrefab.uniqueName, entityId);
        }

        /// <summary>
        /// [Server] Create an entity from the provided prefab.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public T Create<T>(T prefab) where T : BaseEntity
        {
            return Create<T>(prefab, GetFreeId());
        }

        /// <summary>
        /// [Server] Create an entity from the provided network prefab name.
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public BaseEntity Create(string prefabName)
        {
            return Create<BaseEntity>(prefabName);
        }

        /// <summary>
        /// [Server] Create an entity from the provided network prefab name and with the given identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabName"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public T CreateWithId<T>(string prefabName, uint entityId) where T : BaseEntity
        {
            if (_nextFreeId <= entityId)
            {
                _nextFreeId = entityId;
            }

            return Create<T>(prefabName, entityId);
        }

        /// <summary>
        /// [Server] Set the scope of the provided entity for all currently connected players.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="inScope"></param>
        public void SetScopeAll(BaseEntity entity, bool inScope)
        {
            var playerList = Zapnet.Player.Players.List;

            for (var i = 0; i < playerList.Count; i++)
            {
                SetScope(playerList[i], entity, inScope);
            }
        }

        /// <summary>
        /// [Server] Set the scope of the provided entity for a specific player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        /// <param name="inScope"></param>
        public void SetScope(Player player, BaseEntity entity, bool inScope)
        {
            var scopeList = player.ScopeEntities;

            if (inScope)
            {
                if (!scopeList.Contains(entity))
                {
                    Spawn(player, entity);
                }
            }
            else
            {
                if (scopeList.Contains(entity))
                {
                    Despawn(player, entity);
                }
            }
        }

        /// <summary>
        /// [Server] Get whether an entity is scoped for the provided player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsScoped(Player player, BaseEntity entity)
        {
            return player.ScopeEntities.Contains(entity);
        }

        /// <summary>
        /// [Server] Despawn an entity for the provided player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        public void Despawn(Player player, BaseEntity entity)
        {
            if (Zapnet.Network.IsServer)
            {
                var scopeList = player.ScopeEntities;

                if (scopeList.Contains(entity))
                {
                    if (player.IsConnected)
                    {
                        var message = Zapnet.Network.CreateMessage();

                        message.Write((byte)MessageType.DespawnEntity);
                        message.Write(entity.EntityId);

                        Zapnet.Network.SendTo(message, player.Connection, NetDeliveryMethod.ReliableOrdered);
                    }

                    var scopePlayers = entity.ScopePlayers;

                    if (scopePlayers.Contains(player))
                    {
                        scopePlayers.Remove(player);
                        entity.OnScopeCountChanged(scopePlayers.Count);
                        entity.OnPlayerDescoped(player);
                    }

                    scopeList.Remove(entity);
                }
            }
        }

        /// <summary>
        /// [Server] Spawn an entity for the provided player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        public void Spawn(Player player, BaseEntity entity)
        {
            if (Zapnet.Network.IsServer)
            {
                var scopeEntities = player.ScopeEntities;

                if (!scopeEntities.Contains(entity))
                {
                    if (!player.PreventEntitySpawning)
                    {
                        Send(player, entity);
                    }

                    var scopePlayers = entity.ScopePlayers;

                    if (!scopePlayers.Contains(player))
                    {
                        scopePlayers.Add(player);
                        entity.OnScopeCountChanged(scopePlayers.Count);
                        entity.OnPlayerScoped(player);
                    }

                    scopeEntities.Add(entity);
                }
            }
        }

        /// <summary>
        /// [Shared] Find all entities of the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] FindAll<T>() where T : BaseEntity
        {
            return FindObjectsOfType<T>();
        }

        /// <summary>
        /// [Shared] Find the first entity of the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T FindOne<T>() where T : BaseEntity
        {
            return FindObjectOfType<T>();
        }

        /// <summary>
        /// [Shared] Find an entity by its unique identifier.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public BaseEntity Find(uint entityId)
        {
            return Entities.Find(entityId);
        }

        private BaseEntity Add(BaseEntity entity)
        {
            var entityId = entity.EntityId;

            if (!Entities.Exists(entityId))
            {
                var alwaysInScope = entity.alwaysInScope;
                var isServer = Zapnet.Network.IsServer;

                Entities.Add(entityId, entity);
                entity.OnCreated();
                onEntityCreated?.Invoke(entity);

                if (isServer && alwaysInScope)
                {
                    SetScopeAll(entity, true);
                }
            }

            return entity;
        }

        private void Start()
        {
            Zapnet.Player.onInitialDataReceived += OnPlayerInitialDataReceived;

            SceneManager.sceneLoaded += OnSceneLoaded;
            AddSceneEntities();

            _entitiesInRange = new List<BaseEntity>();
            _raycastResults = new List<NetworkRaycastHit>();
            _raycastHits = new RaycastHit[4];
            _hitboxes = new List<NetworkHitbox>();
        }

        private void OnDestroy()
        {
            if (!Zapnet.IsShuttingDown)
            {
                RemoveAll();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AddSceneEntities();
        }

        private void AddSceneEntities()
        {
            var entities = FindObjectsOfType<BaseEntity>();

            foreach (var entity in entities)
            {
                if (entity.EntityId == 0)
                {
                    if (Zapnet.Network.IsServer)
                    {
                        var prefab = entity.NetworkPrefab;

                        if (prefab != null)
                        {
                            var prefabName = prefab.uniqueName;

                            entity.PrefabName = prefabName;
                            entity.PrefabId = Zapnet.Prefab.GetId(prefabName);
                            entity.EntityId = GetFreeId();

                            Add(entity);
                        }
                    }
                    else
                    {
                        Destroy(entity.gameObject);
                    }
                }
            }
        }

        private void OnPlayerInitialDataReceived(Player player)
        {
            if (Zapnet.Network.IsServer)
            {
                var entityList = Entities.List;

                for (var i = 0; i < entityList.Count; i++)
                {
                    var entity = entityList[i];

                    if (entity.alwaysInScope)
                    {
                        Spawn(player, entity);
                    }
                }
            }
        }
    }
}
