/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A wrapper around Lidgren's NetClient object that handles all messages sent to and received
    /// from the server.
    /// </summary>
    public class GameClient
    {
        public delegate void OnConnectionSuccessful();
        public delegate void OnConnectionFailed();

        /// <summary>
        /// Invoked when a connection to the server was successful.
        /// </summary>
        public event OnConnectionSuccessful onConnectionSuccessful;

        /// <summary>
        /// Invoked when a connection to the server has failed.
        /// </summary>
        public event OnConnectionFailed onConnectionFailed;

        private EarlyEventBuffer _earlyEventBuffer;
        private float _nextConnectionTimeout;
        private bool _isConnected;
        private NetIncomingMessage _msg;
        private NetClient _network;

        /// <summary>
        /// Get the underlying NetClient object from Lidgren.
        /// </summary>
        public NetClient Network
        {
            get
            {
                return _network;
            }
        }

        /// <summary>
        /// Get the average round rountrip time in seconds.
        /// </summary>
        public float AverageRoundtripTime
        {
            get
            {
                var connectionToServer = Zapnet.Network.ConnectionToServer;

                if (connectionToServer == null)
                {
                    return 0;
                }

                return connectionToServer.AverageRoundtripTime;
            }
        }

        /// <summary>
        /// Get the average roundtrip time in milliseconds.
        /// </summary>
        public int AverageRoundtripTimeMS
        {
            get
            {
                var connectionToServer = Zapnet.Network.ConnectionToServer;

                if (connectionToServer == null)
                {
                    return 0;
                }

                return Mathf.RoundToInt(connectionToServer.AverageRoundtripTime * 1000f);
            }
        }

        /// <summary>
        /// Get the time offset. This is used to determine the time difference between
        /// this client and the server.
        /// </summary>
        public float TimeOffset
        {
            get
            {
                var connectionToServer = Zapnet.Network.ConnectionToServer;

                if (connectionToServer != null)
                {
                    return connectionToServer.RemoteTimeOffset;
                }

                return 0f;
            }
        }

        internal void Initialize(string serverIp, int serverPort, NetSimulation simulation = default)
        {
            var config = new NetPeerConfiguration(NetSettings.AppId)
            {
                MaximumConnections = 1
            };

            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);

            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.Error);

            if (simulation.latency > 0)
            {
                Debug.Log("[GameClient::Initialize] Setting simulated latency to " + simulation.latency + "ms");
                config.SimulatedMinimumLatency = simulation.latency / 1000f;
            }

            if (simulation.packetLoss > 0f)
            {
                Debug.Log("[GameClient::Initialize] Setting simulated packet loss to " + (simulation.packetLoss * 100f) + "%");
                config.SimulatedLoss = simulation.packetLoss;
            }

            if (_network == null)
            {
                _network = new NetClient(config);
                _network.Start();
            }

            _earlyEventBuffer = new EarlyEventBuffer(_network);

            var credentials = Zapnet.Network.ClientHandler.GetCredentialsPacket();
            var credentialsId = Zapnet.Network.GetIdFromPacket(credentials);

            var hailMessage = _network.CreateMessage();

            hailMessage.Write(NetSettings.AuthId);
            hailMessage.Write(credentialsId);

            credentials.Write(hailMessage);

            _network.Connect(serverIp, serverPort, hailMessage);

            Zapnet.Network.Recycle(credentials);
        }

        internal virtual void Shutdown()
        {
            Zapnet.Network.ClientHandler.OnShutdown();
            _network.Shutdown(string.Empty);
        }

        internal void ReadMessages()
        {
            var shouldRecycleMessage = true;

            if (_nextConnectionTimeout > 0f && Time.time >= _nextConnectionTimeout)
            {
                _nextConnectionTimeout = 0f;
                onConnectionFailed?.Invoke();
            }

            while ((_msg = _network.ReadMessage()) != null)
            {
                shouldRecycleMessage = true;

                switch (_msg.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        {
                            OnStatusChanged(_msg, (NetConnectionStatus)_msg.ReadByte());
                            break;
                        }

                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                        {
                            OnDebug(_msg);
                            break;
                        }

                    case NetIncomingMessageType.WarningMessage:
                        {
                            OnWarning(_msg);
                            break;
                        }

                    case NetIncomingMessageType.Error:
                    case NetIncomingMessageType.ErrorMessage:
                        {
                            OnError(_msg);
                            break;
                        }

                    case NetIncomingMessageType.Data:
                        {
                            var messageType = (MessageType)_msg.ReadByte();

                            if (messageType == MessageType.StateUpdate)
                            {
                                ProcessStateUpdate(_msg);
                            }
                            else if (messageType == MessageType.SpawnEntity)
                            {
                                var spawnTick = _msg.ReadUInt32();
                                var prefabId = _msg.ReadUInt16();
                                var entityId = _msg.ReadUInt32();
                                var entity = Zapnet.Entity.Find(entityId);

                                if (Zapnet.Entity.PreventSpawning || entity)
                                {
                                    break;
                                }

                                var prefab = Zapnet.Prefab.Find(prefabId);

                                if (prefab == null)
                                {
                                    Debug.LogError("[GameServer::ProcessMessages] Cannot spawn an entity with an unknown prefab id: " + prefabId);
                                    break;
                                }

                                entity = Zapnet.Entity.Create(prefab.uniqueName, entityId);

                                if (entity)
                                {
                                    entity.SpawnTick = spawnTick;
                                    entity.ReadSpawn(_msg);
                                    entity.OnSpawned();
                                }
                                else
                                {
                                    Debug.Log("[GameClient::ReadMessages] Unable to create entity with prefab name: " + prefab.uniqueName);
                                }
                            }
                            else if (messageType == MessageType.DespawnEntity)
                            {
                                var entityId = _msg.ReadUInt32();
                                var entity = Zapnet.Entity.Find(entityId);

                                if (entity)
                                {
                                    entity.OnDespawned();
                                    Zapnet.Entity.Remove(entity);
                                }
                            }
                            else if (messageType == MessageType.PlayerConnected)
                            {
                                Zapnet.Player.Add(_msg.ReadUInt32());
                            }
                            else if (messageType == MessageType.InitialData)
                            {
                                var playerList = PlayerListPacket.Read(_msg);

                                Zapnet.Player.LocalPlayerId = playerList.LocalPlayerId;

                                var players = playerList.PlayerIds;

                                for (var i = 0; i < players.Count; i++)
                                {
                                    Zapnet.Player.Add(players[i]);
                                }

                                var prefabList = PrefabListPacket.Read(_msg);

                                foreach (var kv in prefabList.Prefabs)
                                {
                                    Zapnet.Prefab.Add(kv.Key, kv.Value);
                                }

                                Zapnet.Network.ClientHandler.ReadInitialData(_msg);

                                var response = _network.CreateMessage();
                                response.Write((byte)MessageType.InitialData);
                                _network.SendMessage(response, NetDeliveryMethod.ReliableOrdered);

                                var localPlayer = Zapnet.Player.LocalPlayer;

                                if (localPlayer != null)
                                {
                                    Zapnet.Player.InvokeInitialDataReceived(localPlayer);
                                }
                            }
                            else if (messageType == MessageType.PlayerDisconnected)
                            {
                                var playerId = _msg.ReadUInt32();
                                var player = Zapnet.Player.Find(playerId);

                                if (player != null)
                                {
                                    Zapnet.Player.Remove(player);
                                }
                            }
                            else if (messageType == MessageType.Event)
                            {
                                var eventType = (NetworkEventType)_msg.ReadByte();
                                var sendTick = _msg.ReadUInt32();
                                var packetId = _msg.ReadByte();
                                uint entityId = 0;
                                var entity = (BaseEntity)null;
                                var data = (BaseEventData)Zapnet.Network.CreatePacket(packetId);

                                if (eventType == NetworkEventType.Entity)
                                {
                                    entityId = _msg.ReadUInt32();
                                    entity = Zapnet.Entity.Find(entityId);
                                }

                                if (data != null)
                                {
                                    data.SendTick = sendTick;
                                    data.Entity = entity;

                                    if (entityId > 0 && entity == null)
                                    {
                                        if (_earlyEventBuffer.Add(entityId, packetId, data, _msg))
                                        {
                                            shouldRecycleMessage = false;
                                        }
                                        else
                                        {
                                            Debug.Log("[GameClient::ProcessMessages] Unable to call event " + data.GetType().Name + " because the entity " + entityId + " does not exist yet!");
                                            data.Recycle();
                                        }

                                        break;
                                    }
                                    else
                                    {
                                        if (!data.Read(_msg))
                                        {
                                            data.Recycle();
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log("[GameClient::ProcessMessages] Unable to call event with unknown packet with id: " + packetId);
                                    break;
                                }

                                if (entity != null)
                                {
                                    entity.Call(packetId, data);
                                }
                                else
                                {
                                    Zapnet.Network.Call(packetId, data);
                                }

                                data.Recycle();
                            }

                            break;
                        }

                    default:
                        {
                            OnEvent(_msg);
                            break;
                        }
                }

                if (shouldRecycleMessage)
                {
                    _network.Recycle(_msg);
                }
            }
        }

        private void ProcessStateUpdate(NetIncomingMessage buffer)
        {
            var serverTick = buffer.ReadUInt32();

            Zapnet.Network.UpdateOffset(serverTick);

            var entityCount = buffer.ReadInt32();

            if (entityCount > 0)
            {
                for (var i = 0; i < entityCount; i++)
                {
                    var entityId = buffer.ReadUInt32();

                    if (entityId > 0)
                    {
                        var entity = Zapnet.Entity.Find(entityId);

                        if (entity)
                        {
                            entity.ProcessStateUpdate(buffer, serverTick);
                        }
                        else
                        {
                            Debug.Log("[GameClient::ProcessStateUpdate] Unable to find entity by id #" + entityId);
                            break;
                        }
                    }
                }
            }

            _earlyEventBuffer.ClearStaleEvents();
        }

        private void OnStatusChanged(NetIncomingMessage msg, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.Connected:
                    {
                        Debug.Log("[GameClient::OnStatusChanged] Connection " + msg.SenderConnection + " @ " + msg.SenderEndPoint);

                        _nextConnectionTimeout = 0f;
                        _isConnected = true;

                        OnConnected(msg);

                        break;
                    }

                case NetConnectionStatus.Disconnected:
                    {
                        if (!_isConnected || _nextConnectionTimeout > 0f)
                        {
                            _nextConnectionTimeout = 0f;
                            onConnectionFailed?.Invoke();
                        }

                        OnDisconnected(msg);

                        break;
                    }
                case NetConnectionStatus.InitiatedConnect:
                    {
                        _nextConnectionTimeout = Time.time + 5f;
                        _isConnected = false;

                        break;
                    }
                default:
                    {
                        Debug.Log("[GameClient::OnStatusChanged] " + status + " from " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
                        break;
                    }
            }
        }

        private void OnEvent(NetIncomingMessage msg)
        {

        }

        private void OnDebug(NetIncomingMessage msg)
        {

        }

        private void OnWarning(NetIncomingMessage msg)
        {
            //Debug.LogWarning(msg.ReadString());
        }

        private void OnError(NetIncomingMessage msg)
        {
            Debug.LogError(msg.ReadString());
        }

        private void OnConnected(NetIncomingMessage msg)
        {
            Debug.Log("[GameClient::OnConnected] " + msg.SenderConnection + " @ " + msg.SenderEndPoint);

            var connectMessage = _network.CreateMessage();

            connectMessage.Write((byte)MessageType.PlayerConnected);
            connectMessage.Write(Zapnet.Entity.PreventSpawning);

            _network.SendMessage(connectMessage, NetDeliveryMethod.ReliableOrdered);

            onConnectionSuccessful?.Invoke();
        }

        private void OnDisconnected(NetIncomingMessage msg)
        {
            Zapnet.Network.ClientHandler.OnDisconnected();
            Debug.Log("[GameClient::OnDisconnected] " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
        }
    }
}
