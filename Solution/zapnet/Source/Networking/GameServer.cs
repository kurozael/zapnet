/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System;
using System.Collections.Generic;
using System.Net;
using Lidgren.Network;
using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// A wrapper around Lidgren's NetServer object that handles all messages sent to and received
    /// from clients.
    /// </summary>
    public class GameServer
    {
        private NetServer _network;
        private NetIncomingMessage _msg;

        /// <summary>
        /// Get the underlying NetServer object from Lidgren.
        /// </summary>
        public NetServer Network
        {
            get
            {
                return _network;
            }
        }

        internal void Initialize(IPAddress address, int port, NetSimulation simulation = default)
        {
            var config = new NetPeerConfiguration(NetSettings.AppId)
            {
                MaximumConnections = 256,
                LocalAddress = address,
                Port = port
            };

            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);

            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.Error);

            if (simulation.latency > 0)
            {
                Debug.Log("[GameServer::Initialize] Setting simulated latency to " + simulation.latency + "ms");
                config.SimulatedMinimumLatency = simulation.latency / 1000f;
            }

            if (simulation.packetLoss > 0f)
            {
                Debug.Log("[GameServer::Initialize] Setting simulated packet loss to " + (simulation.packetLoss * 100f) + "%");
                config.SimulatedLoss = simulation.packetLoss;
            }

            _network = new NetServer(config);
            _network.Start();
        }

        internal void Shutdown()
        {
            _network.Shutdown(string.Empty);
        }

        internal void ReadMessages()
        {
            while ((_msg = _network.ReadMessage()) != null)
            {
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
                            var sendingPlayer = Zapnet.Player.Find(_msg.SenderConnection);
                            var messageType = (MessageType)_msg.ReadByte();

                            if (messageType == MessageType.PlayerConnected)
                            {
                                var serverHandler = Zapnet.Network.ServerHandler;

                                if (!CheckLoginCredentials(_msg.SenderConnection))
                                {
                                    break;
                                }

                                var credentials = (_msg.SenderConnection.Tag as INetworkPacket);

                                if (sendingPlayer == null)
                                {
                                    sendingPlayer = Zapnet.Player.Add(_msg.SenderConnection, credentials);
                                }
                                else
                                {
                                    Debug.Log("[GameServer::ProcessMessages] A player has tried to connect while already being connected: " + sendingPlayer.PlayerId);
                                    break;
                                }

                                var preventSpawning = _msg.ReadBoolean();

                                sendingPlayer.PreventEntitySpawning = preventSpawning;

                                var prefabList = new PrefabListPacket
                                {
                                    Prefabs = Zapnet.Prefab.GetNetworkTable()
                                };

                                var playerList = new PlayerListPacket
                                {
                                    LocalPlayerId = sendingPlayer.PlayerId,
                                    PlayerIds = new List<uint>()
                                };

                                var allPlayers = Zapnet.Player.Players.List;

                                for (var i = 0; i < allPlayers.Count; i++)
                                {
                                    var player = allPlayers[i];

                                    if (player.IsLocalPlayer || player.IsConnected)
                                    {
                                        playerList.PlayerIds.Add(player.PlayerId);
                                    }
                                }

                                var initialData = _network.CreateMessage();
                                initialData.Write((byte)MessageType.InitialData);

                                playerList.Write(initialData);
                                prefabList.Write(initialData);

                                serverHandler.WriteInitialData(sendingPlayer, initialData);

                                _network.SendMessage(initialData, sendingPlayer.Connection, NetDeliveryMethod.ReliableOrdered);

                                Zapnet.Player.InvokeInitialDataSent(sendingPlayer);

                                var connectMessage = _network.CreateMessage();
                                connectMessage.Write((byte)MessageType.PlayerConnected);
                                connectMessage.Write(sendingPlayer.PlayerId);
                                _network.SendToAll(connectMessage, NetDeliveryMethod.ReliableOrdered);

                                serverHandler.OnPlayerConnected(sendingPlayer, credentials);
                            }
                            else if (messageType == MessageType.PreventSpawning)
                            {
                                if (sendingPlayer != null)
                                {
                                    var preventSpawning = _msg.ReadBoolean();

                                    sendingPlayer.PreventEntitySpawning = preventSpawning;

                                    if (!preventSpawning)
                                    {
                                        foreach (var entity in sendingPlayer.ScopeEntities)
                                        {
                                            Zapnet.Entity.Send(sendingPlayer, entity);
                                        }
                                    }
                                }
                            }
                            else if (messageType == MessageType.InitialData)
                            {
                                if (sendingPlayer == null)
                                {
                                    break;
                                }

                                Zapnet.Player.InvokeInitialDataReceived(sendingPlayer);
                            }
                            else if (messageType == MessageType.Event)
                            {
                                var eventType = (NetworkEventType)_msg.ReadByte();
                                var sendTick = _msg.ReadUInt32();
                                var packetId = _msg.ReadByte();
                                var entity = (BaseEntity)null;

                                if (eventType == NetworkEventType.Entity)
                                {
                                    var entityId = _msg.ReadUInt32();
                                    entity = Zapnet.Entity.Find(entityId);

                                    if (entity == null)
                                    {
                                        Debug.Log("[GameServer::ProcessMessages] Unable to call event for a non-existant entity#" + entityId);
                                        break;
                                    }
                                }

                                var data = (BaseEventData)Zapnet.Network.CreatePacket(packetId);

                                if (data != null)
                                {
                                    data.SendTick = sendTick;
                                    data.Sender = sendingPlayer;
                                    data.Entity = entity;

                                    if (!data.Read(_msg))
                                    {
                                        data.Recycle();
                                        break;
                                    }
                                }
                                else
                                {
                                    Debug.Log("[GameServer::ProcessMessages] Unable to call event with unknown packet#" + packetId);
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

                    case NetIncomingMessageType.ConnectionApproval:
                        {
                            OnConnectionApproval(_msg);
                            break;
                        }

                    default:
                        {
                            OnEvent(_msg);
                            break;
                        }
                }

                _network.Recycle(_msg);
            }
        }

        private bool CheckLoginCredentials(NetConnection connection)
        {
            var credentials = (connection.Tag as INetworkPacket);

            if (credentials == null)
                                {
                Debug.Log("[GameServer::CheckLoginCredentials] A player has tried to connect with an unusual or missing login packet!");
                connection.Disconnect("INVALID_LOGIN");
                return false;
            }

            return true;
        }

        private List<T> List<T>()
        {
            throw new NotImplementedException();
        }

        private void OnStatusChanged(NetIncomingMessage msg, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.Connected:
                    {
                        Debug.Log("[GameServer::OnStatusChanged] Connection " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
                        break;
                    }

                case NetConnectionStatus.Disconnected:
                    {
                        OnDisconnected(msg);
                        break;
                    }

                default:
                    {
                        Debug.Log("[GameServer::OnStatusChanged] " + status + " from " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
                        break;
                    }
            }
        }

        private void OnConnectionApproval(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            var secret = msg.ReadString();
            var packetId = msg.ReadByte();
            var packet = Zapnet.Network.CreatePacket(packetId);

            packet.Read(msg);

            if (secret == NetSettings.AuthId)
            {
                if (packet == null || !Zapnet.Network.ServerHandler.CanPlayerAuth(packet))
                {
                    sender.Deny();

                    Debug.Log("[GameServer::ProcessMessages] A player has attempted to auth, but has been rejected!");
                }
                else
                {
                    sender.Tag = packet;
                    sender.Approve();

                    Debug.Log("[GameServer::OnConnectionApproval] Sender " + msg.SenderEndPoint + " connection was approved.");
                }
            }
            else
            {
                sender.Deny();

                Debug.Log("[GameServer::OnConnectionApproval] Sender " + msg.SenderEndPoint + " connection was denied!");
            }
        }

        private void OnEvent(NetIncomingMessage msg)
        {
            Debug.Log("[GameServer::OnEvent] " + msg.MessageType);
        }

        private void OnDebug(NetIncomingMessage msg)
        {
            Debug.Log(msg.ReadString());
        }

        private void OnWarning(NetIncomingMessage msg)
        {
            Debug.LogWarning(msg.ReadString());
        }

        private void OnError(NetIncomingMessage msg)
        {
            Debug.LogError(msg.ReadString());
        }

        private void OnDisconnected(NetIncomingMessage msg)
        {
            var player = Zapnet.Player.Find(msg.SenderConnection);

            if (player != null)
            {
                var disconnectMessage = _network.CreateMessage();
                disconnectMessage.Write((byte)MessageType.PlayerDisconnected);
                disconnectMessage.Write(player.PlayerId);
                _network.SendToAll(disconnectMessage, NetDeliveryMethod.ReliableOrdered);

                Zapnet.Network.ServerHandler.OnPlayerDisconnected(player);

                Zapnet.Player.Remove(player);
            }

            Debug.Log("[GameServer::OnDisconnected] Disconnection: " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
        }

        internal void AddStateUpdate()
        {
            var playerList = Zapnet.Player.Players.List;
            var localTick = Zapnet.Network.LocalTick;
            uint frozenId = 0;
            var channel = Zapnet.Network.GetChannel(NetChannel.EntityStates);

            for (var i = 0; i < playerList.Count; i++)
            {
                var player = playerList[i];

                if (player.IsConnected)
                {
                    var scopeList = player.ScopeEntities;
                    var entityCount = scopeList.Count;

                    var message = _network.CreateMessage(entityCount * 256);
                    message.Write((byte)MessageType.StateUpdate);
                    message.Write(localTick);
                    message.Write(entityCount);

                    foreach (var entity in scopeList)
                    {
                        if (!entity.IsFrozen)
                        {
                            message.Write(entity.EntityId);
                            entity.AddStateUpdate(message, localTick);
                        }
                        else
                        {
                            message.Write(frozenId);
                        }
                    }

                    _network.SendMessage(message, player.Connection, NetDeliveryMethod.UnreliableSequenced, channel);
                }
            }
        }
    }
}