/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A behaviour to manage all network operations.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        /// <summary>
        /// Called whenever the network ticks.
        /// </summary>
        public event Action onTick;

        private Dictionary<byte, RemoteCallAttribute> _remoteCallAttributes;
        private TwoWayDictionary<byte, MethodInfo> _remoteCallMethods;
        private Dictionary<byte, List<EventListener>> _eventListeners;
        private Dictionary<Type, Queue<INetworkPoolable>> _packetPool;
        private Dictionary<Type, Dictionary<int, int>> _sequenceChannels;
        private TwoWayDictionary<byte, Type> _packetTypes;
        private NetConnection _connectionToServer;
        private ushort _nextPacketId;
        private int _nextSequenceChannel;

        /// <summary>
        /// Get the current server handler implementation.
        /// </summary>
        public IServerHandler ServerHandler { get; internal set; }

        /// <summary>
        /// Get the current client handler implementation.
        /// </summary>
        public IClientHandler ClientHandler { get; internal set; }

        /// <summary>
        /// Get the underlying game server object.
        /// </summary>
        public GameServer Server { get; private set; }

        /// <summary>
        /// Get the underlying game client object.
        /// </summary>
        public GameClient Client { get; private set; }

        /// <summary>
        /// Get the underlying NetPeer object from Lidgren.
        /// </summary>
        public NetPeer Network
        {
            get
            {
                if (Server != null)
                {
                    return Server.Network;
                }
                else
                {
                    return Client.Network;
                }
            }
        }

        /// <summary>
        /// Get whether a network connection currently exists.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (Server != null || Client != null);
            }
        }

        /// <summary>
        /// Get the underlying Lidgren NetConnection to the server.
        /// </summary>
        public NetConnection ConnectionToServer
        {
            get
            {
                if (_connectionToServer == null && Client.Network.Connections.Count > 0)
                {
                    _connectionToServer = Client.Network.Connections[0];
                }

                return _connectionToServer;
            }
        }

        /// <summary>
        /// Get whether we are a listen server (a client connected to a server sharing the same process.)
        /// </summary>
        public bool IsListenServer { get; internal set; }

        /// <summary>
        /// Get whether we are the server.
        /// </summary>
        public bool IsServer { get { return (Server != null); } }

        /// <summary>
        /// Get whether we are a client.
        /// </summary>
        public bool IsClient { get { return (Client != null); } }

        private uint _tickOffset = 0;

        /// <summary>
        /// Get a fixed delta time value representing the time it takes to perform a single tick.
        /// </summary>
        public float FixedDeltaTime
        {
            get
            {
                return Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Get the estimated amount of ticks it takes to perform a roundtrip.
        /// </summary>
        public uint RoundtripTickTime
        {
            get
            {
                var rttInTicks = NetSettings.TickRate * Client.AverageRoundtripTime;
                return (uint)rttInTicks;
            }
        }

        /// <summary>
        /// Get the estimated time on the server in seconds.
        /// </summary>
        public double ServerTime
        {
            get
            {
                if (Client != null)
                {
                    var connectionToServer = ConnectionToServer;

                    if (connectionToServer != null)
                    {
                        return connectionToServer.GetRemoteTime(NetTime.Now);
                    }
                    else
                    {
                        return 0f;
                    }
                }
                else
                {
                    return NetTime.Now;
                }
            }
        }

        /// <summary>
        /// Get the estimated server tick value.
        /// </summary>
        public uint ServerTick
        {
            get
            {
                if (IsServer)
                {
                    return LocalTick;
                }
                else
                {
                    return LocalTick + _tickOffset;
                }
            }
        }

        /// <summary>
        /// Get the local tick value (not synchronized.)
        /// </summary>
        public uint LocalTick { get; private set; } = 0;

        /// <summary>
        /// Send an outgoing message to the provided recipients.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recipients"></param>
        /// <param name="method"></param>
        /// <param name="channel"></param>
        public void SendTo(NetOutgoingMessage buffer, IList<NetConnection> recipients, NetDeliveryMethod method, int channel = 0)
        {
            if (Server != null)
            {
                Server.Network.SendMessage(buffer, recipients, method, channel);
            }
        }

        /// <summary>
        /// Send an outgoing message to the provided recipient.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recipient"></param>
        /// <param name="method"></param>
        /// <param name="channel"></param>
        public void SendTo(NetOutgoingMessage buffer, NetConnection recipient, NetDeliveryMethod method, int channel = 0)
        {
            if (Server != null)
            {
                recipient.SendMessage(buffer, method, channel);
            }
        }

        /// <summary>
        /// Send an outgoing message to all recipients except the provided one.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="except"></param>
        /// <param name="method"></param>
        /// <param name="channel"></param>
        public void SendToAll(NetOutgoingMessage buffer, NetConnection except, NetDeliveryMethod method, int channel = 0)
        {
            if (Server != null)
            {
                Server.Network.SendToAll(buffer, except, method, channel);
            }
        }

        /// <summary>
        /// Send an outgoing message to all recipients.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="method"></param>
        /// <param name="channel"></param>
        public void SendToAll(NetOutgoingMessage buffer, NetDeliveryMethod method, int channel = 0)
        {
            if (Server != null)
            {
                Server.Network.SendToAll(buffer, null, method, channel);
            }
        }

        /// <summary>
        /// Send an outgoing message to the server.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="method"></param>
        /// <param name="channel"></param>
        public void SendToServer(NetOutgoingMessage buffer, NetDeliveryMethod method, int channel = 0)
        {
            if (Client != null)
            {
                Client.Network.SendMessage(buffer, method, channel);
            }
        }

        /// <summary>
        /// Register a new channel from the provided custom enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        public void RegisterChannel<T>(T identifier) where T : struct, IConvertible
        {
            var type = typeof(T);

            if (!_sequenceChannels.TryGetValue(type, out var map))
            {
                map = _sequenceChannels[type] = new Dictionary<int, int>();
            }

            var channelId = _nextSequenceChannel++; ;
            var number = UnsafeUtility.EnumToInt(identifier);

            Debug.Log("[NetworkManager::RegisterChannel] Registered a new channel for " + identifier.ToString() + " to #" + channelId);

            map[number] = channelId;
        }

        /// <summary>
        /// Get a unique channel integer from the provided custom enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public int GetChannel<T>(T identifier) where T : struct, IConvertible
        {
            var type = typeof(T);

            if (_sequenceChannels.TryGetValue(type, out var map))
            {
                var number = UnsafeUtility.EnumToInt(identifier);

                if (map.TryGetValue(number, out var channel))
                {
                    return channel;
                }
            }

            return 0;
        }

        /// <summary>
        /// Get the unique remote call identifier from the provided method info.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public byte GetRemoteCallId(MethodInfo method)
        {
            if (_remoteCallMethods.TryGetKey(method, out var id))
            {
                return id;
            }

            return 0;
        }

        /// <summary>
        /// Get the remote call attribute for the provided unique remote call identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RemoteCallAttribute GetRemoteCallAttribute(byte id)
        {
            if (_remoteCallAttributes.ContainsKey(id))
            {
                return _remoteCallAttributes[id];
            }

            return null;
        }

        /// <summary>
        /// Get the method info for the provided unique remote call identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MethodInfo GetRemoteCallMethod(byte id)
        {
            if (_remoteCallMethods.TryGetValue(id, out var method))
            {
                return method;
            }

            return null;
        }

        /// <summary>
        /// Register a new packet type and optionally precache a number of instances to be pooled. Multiple calls
        /// of this method MUST be called in the same order on the server and client.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="precache"></param>
        public void RegisterPacket<T>(int precache = 10) where T : INetworkPacket
        {
            _nextPacketId++;

            var packetType = typeof(T);
            var packetId = (byte)_nextPacketId;

            _packetTypes.Add(packetId, packetType);

            if (precache > 0)
            {
                Debug.Log("[NetworkManager::RegisterPacket] " + packetId + " -> " + packetType.Name + " (Precache x" + precache + ")");

                if (!packetType.IsSubclassOf(typeof(BaseEventData)))
                {
                    var precached = new List<INetworkPacket>();

                    for (var i = 0; i < precache; i++)
                    {
                        precached.Add(CreatePacket(packetId));
                    }

                    for (var i = 0; i < precached.Count; i++)
                    {
                        Recycle(precached[i]);
                    }
                }
                else
                {
                    var precached = new List<NetworkEvent<T>>();

                    for (var i = 0; i < precache; i++)
                    {
                        precached.Add(CreateEvent<T>());
                    }

                    for (var i = 0; i < precached.Count; i++)
                    {
                        precached[i].Recycle();
                    }
                }
            }
            else
            {
                Debug.Log("[NetworkManager::RegisterPacket] " + packetId + " -> " + packetType.Name);
            }
        }

        /// <summary>
        /// Recycle a poolable network object.
        /// </summary>
        /// <param name="poolable"></param>
        public void Recycle(INetworkPoolable poolable)
        {
            var packetType = poolable.GetType();

            if (!_packetPool.TryGetValue(packetType, out var pool))
            {
                pool = _packetPool[packetType] = new Queue<INetworkPoolable>();
            }

            pool.Enqueue(poolable);

            poolable.OnRecycled();
        }

        /// <summary>
        /// Fetch a poolable network object with the provided type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public INetworkPoolable Fetch(Type type)
        {
            if (_packetPool.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var packet = pool.Dequeue();
                packet.OnFetched();
                return packet;
            }

            var instance = (INetworkPoolable)Activator.CreateInstance(type);
            instance.OnFetched();
            return instance;
        }

        /// <summary>
        /// Fetch a poolable network object with the provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Fetch<T>()
        {
            return (T)Fetch(typeof(T));
        }

        /// <summary>
        /// Create a network packet with the provided unique packet identifier.
        /// </summary>
        /// <param name="packetId"></param>
        /// <returns></returns>
        public INetworkPacket CreatePacket(byte packetId)
        {
            if (!_packetTypes.TryGetValue(packetId, out var packet))
            {
                Debug.LogError("[NetworkManager::GetPacketFromId] Unable to locate packet type for id: " + packetId);
                return null;
            }

            return (INetworkPacket)Fetch(packet);
        }

        /// <summary>
        /// Create a network packet with the provided network packet type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CreatePacket<T>() where T : INetworkPacket
        {
            var packetType = typeof(T);

            if (!_packetTypes.ContainsValue(packetType))
            {
                Debug.LogError("[NetworkManager::GetPacketFromId] Unable to create an unregistered packet type: " + packetType.Name);
                return default;
            }

            return Fetch<T>();
        }

        /// <summary>
        /// Get the unique packet identifier for the provided network packet type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public byte GetPacketId<T>() where T : INetworkPacket
        {
            var packetType = typeof(T);

            if (_packetTypes.ContainsValue(packetType))
            {
                return _packetTypes.GetByValue(packetType);
            }

            return 0;
        }

        /// <summary>
        /// Get the unique packet identifier for the provided network packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public byte GetIdFromPacket(INetworkPacket packet)
        {
            var packetType = packet.GetType();

            if (_packetTypes.ContainsValue(packetType))
            {
                return _packetTypes.GetByValue(packetType);
            }

            return 0;
        }

        /// <summary>
        /// Create a new event (fetched from a pool) for the provided event data type. Optionally pass
        /// in existing data to use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public NetworkEvent<T> CreateEvent<T>(T data = default) where T : INetworkPacket
        {
            var evnt = (NetworkEvent<T>)Fetch(typeof(NetworkEvent<T>));

            if (EqualityComparer<T>.Default.Equals(data, default))
            {
                evnt.Data = Zapnet.Network.CreatePacket<T>();
            }
            else
            {
                evnt.Data = data;
            }
            
            return evnt;
        }

        /// <summary>
        /// Fire an event for the provided unique packet identifier and data.
        /// </summary>
        /// <param name="packetId"></param>
        /// <param name="data"></param>
        public void Call(byte packetId, INetworkPacket data)
        {
            if (_eventListeners.TryGetValue(packetId, out var list))
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    list[i].callback(data);
                }
            }
        }

        /// <summary>
        /// Subscribe to when the provided network packet type is received.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void Subscribe<T>(OnProcessEvent<T> callback) where T : BaseEventData
        {
            var packetId = GetPacketId<T>();

            if (packetId == 0)
            {
                Debug.LogError("[NetworkManager::AddListener] Unable to add an event listener for an unknown event type: " + typeof(T));
                return;
            }

            if (!_eventListeners.TryGetValue(packetId, out var listeners))
            {
                listeners = _eventListeners[packetId] = new List<EventListener>();
            }

            var listener = new EventListener
            {
                original = callback,
                callback = (data) =>
                {
                    callback(data as T);
                }
            };

            listeners.Add(listener);
        }

        /// <summary>
        /// Unsubscribe from when the provided network packet type is received.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public void Unsubscribe<T>(OnProcessEvent<T> callback) where T : BaseEventData
        {
            var packetId = GetPacketId<T>();

            if (packetId == 0)
            {
                Debug.LogError("[NetworkManager::RemoveListener] Unable to remove an event listener for an unknown event type: " + typeof(T));
                return;
            }

            if (_eventListeners.TryGetValue(packetId, out var list))
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].original.GetHashCode() == callback.GetHashCode())
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Create a new Lidgren outgoing message with an initial capacity of 256 bytes.
        /// </summary>
        /// <returns></returns>
        public NetOutgoingMessage CreateMessage()
        {
            return Network.CreateMessage(256);
        }

        internal void UpdateOffset(uint serverTick)
        {
            var rttInTicks = NetSettings.TickRate * (Client.AverageRoundtripTime / 2f);
            _tickOffset = (serverTick - LocalTick) + (uint)rttInTicks;
        }

        /// <summary>
        /// Start hosting a server with the provided server handler. This will automatically bind to
        /// any available network address.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="handler"></param>
        /// <param name="simulation"></param>
        public void Host(int port, IServerHandler handler, NetSimulation simulation = default)
        {
            Host(IPAddress.Any, port, handler, simulation);
        }

        /// <summary>
        /// Start hosting a server with the provided server handler. You can manually specify
        /// which network address to bind to.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="handler"></param>
        /// <param name="simulation"></param>
        public void Host(IPAddress address, int port, IServerHandler handler, NetSimulation simulation = default)
        {
            ServerHandler = handler;

            Server = new GameServer();
            Server.Initialize(address, port, simulation);

            Zapnet.InvokeServerStarted();
        }

        /// <summary>
        /// Connect to an existing server with the provided client handler.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="handler"></param>
        /// <param name="simulation"></param>
        public void Connect(string ip, int port, IClientHandler handler, NetSimulation simulation = default)
        {
            if (Server != null)
            {
                Debug.LogError("[NetworkManager::Connect] You cannot make a connection while hosting a server. You might be looking for MakeListenServer.");
                return;
            }

            ClientHandler = handler;

            if (Client == null)
            {
                Client = new GameClient();
            }

            Client.Initialize(ip, port, simulation);

            Zapnet.InvokeClientStarted();
        }

        /// <summary>
        /// Make this server a listen server. This automatically adds a new player that represents the server. This should only
        /// be called after the scene has been loaded, and the server has finished loading prefabs. Essentially, you should only
        /// call this at the point you would otherwise allow regular players to connect.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void MakeListenServer(IClientHandler handler)
        {
            if (IsServer)
            {
                IsListenServer = true;
                ClientHandler = handler;

                var credentials = ClientHandler.GetCredentialsPacket();
                var playerId = Zapnet.Player.GetFreeId();

                Zapnet.Player.LocalPlayerId = playerId;

                var player = Zapnet.Player.Add(playerId, null, credentials);

                ServerHandler.OnPlayerConnected(player, credentials);
                ServerHandler.OnInitialDataReceived(player, credentials);
            }
        }

        /// <summary>
        /// Get whether the current tick sits on the provided priority number. For example, calling
        /// IsTick(1) will return true for every tick, IsTick(3) will return true for every 3rd tick,
        /// and so on.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool IsTick(uint priority)
        {
            return (LocalTick % priority == 0);
        }

        internal void Initialize()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Physics.defaultSolverIterations = 10;
            Physics.autoSyncTransforms = true;
            Physics.autoSimulation = false;
            Time.fixedDeltaTime = (1f / NetSettings.TickRate);

            _sequenceChannels = new Dictionary<Type, Dictionary<int, int>>();
            _eventListeners = new Dictionary<byte, List<EventListener>>();
            _remoteCallAttributes = new Dictionary<byte, RemoteCallAttribute>();
            _remoteCallMethods = new TwoWayDictionary<byte, MethodInfo>();
            _packetTypes = new TwoWayDictionary<byte, Type>();
            _packetPool = new Dictionary<Type, Queue<INetworkPoolable>>();

            RegisterPacket<SynchronizeEvent>();
            RegisterPacket<ControlGainedEvent>();
            RegisterPacket<ControlLostEvent>();
            RegisterPacket<RemoteCallEvent>();

            RegisterChannel(NetChannel.EntityStates);
            RegisterChannel(NetChannel.PlayerInput);
            RegisterChannel(NetChannel.SyncVars);

            FindRemoteCallMethods();
        }

        private void FindRemoteCallMethods()
        {
            byte remoteCallId = 0;
            var subsystemType = typeof(EntitySubsystem);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(subsystemType))
                    {
                        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                        foreach (var method in methods)
                        {
                            var attributes = method.GetCustomAttributes(typeof(RemoteCallAttribute), true);

                            if (attributes.Length > 0)
                            {
                                var id = ++remoteCallId;

                                Debug.Log("[NetworkManager::FindRemoteCallMethods] Adding RPC#" + id + " for " + type.FullName + ":" + method.Name);

                                _remoteCallAttributes.Add(id, (attributes[0] as RemoteCallAttribute));
                                _remoteCallMethods.Add(id, method);
                            }
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (Client != null)
            {
                Client.Shutdown();
                Client = null;
            }

            if (Server != null)
            {
                Server.Shutdown();
                Server = null;
            }
        }

        private void FixedUpdate()
        {
            UniversalTime.Update();

            var fixedDeltaTime = Time.fixedDeltaTime;

            Physics.Simulate(fixedDeltaTime);

            Client?.ReadMessages();
            Server?.ReadMessages();

            if (IsTick(NetSettings.StatePriority))
            {
                Server?.AddStateUpdate();
            }

            var entityList = Zapnet.Entity.Entities.List;

            for (var i = entityList.Count - 1; i >= 0; i--)
            {
                var entity = entityList[i];

                if (entity.ShouldTick())
                {
                    entity.Tick();
                }
            }

            onTick?.Invoke();

            LocalTick++;
        }

        private void Start()
        {
            Zapnet.Player.onInitialDataReceived += OnPlayerInitialDataReceived;
        }

        private void OnPlayerInitialDataReceived(Player player)
        {
            if (ServerHandler != null)
            {
                ServerHandler.OnInitialDataReceived(player, player.LoginCredentials);
            }
        }
    }
}
