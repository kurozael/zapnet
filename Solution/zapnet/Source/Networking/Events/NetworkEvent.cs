/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace zapnet
{
    /// <summary>
    /// Represents an event that will be sent across the network.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class NetworkEvent<T> : INetworkPoolable where T : INetworkPacket
    {
        protected T _data;
        protected int _channel;
        protected byte _packetId;
        protected BaseEntity _entity;
        protected HashSet<Player> _ignored;
        protected HashSet<Player> _recipients;
        protected List<NetConnection> _sendList;
        protected NetworkEventType _type;
        protected NetDeliveryMethod _method;
        protected bool _autoRecipients;
        protected bool _recycleData;
        protected bool _recycleSelf;

        /// <summary>
        /// The type of this network event.
        /// </summary>
        public NetworkEventType Type => _type;

        /// <summary>
        /// The entity that this event will be invoked on.
        /// </summary>
        public BaseEntity Entity => _entity;

        /// <summary>
        /// Whether or not to automatically add all connected players as recipients
        /// if no recipients are manually added.
        /// </summary>
        public bool AutoRecipients
        {
            get => _autoRecipients;
            set => _autoRecipients = value;
        }

        /// <summary>
        /// Whether or not to recycle data attached to this event.
        /// </summary>
        public bool RecycleData
        {
            get => _recycleData;
            set => _recycleData = value;
        }

        /// <summary>
        /// Whether or not to recycle this event.
        /// </summary>
        public bool RecycleSelf
        {
            get => _recycleSelf;
            set => _recycleSelf = value;
        }

        /// <summary>
        /// Get the data object that will be sent with this event.
        /// </summary>
        public T Data
        {
            get => _data;
            set => _data = value;
        }

        public NetworkEvent()
        {
            _recipients = new HashSet<Player>();
            _packetId = Zapnet.Network.GetPacketId<T>();
            _sendList = new List<NetConnection>();
            _ignored = new HashSet<Player>();

            Reset();
        }

        /// <summary>
        /// Reset the event to its default settings.
        /// </summary>
        public void Reset()
        {
            _recipients.Clear();
            _sendList.Clear();
            _ignored.Clear();

            _autoRecipients = true;
            _recycleData = true;
            _recycleSelf = true;
            _method = NetDeliveryMethod.ReliableOrdered;
            _channel = 0;
            _entity = null;
            _data = default;
            _type = NetworkEventType.Global;
        }

        /// <summary>
        /// Set the delivery method of this event.
        /// </summary>
        /// <param name="method"></param>
        public void SetDeliveryMethod(NetDeliveryMethod method)
        {
            _method = method;
        }

        /// <summary>
        /// Set the recipients of this event.
        /// </summary>
        /// <param name="recipients"></param>
        public void SetRecipients(HashSet<Player> recipients)
        {
            _recipients = recipients;
        }

        /// <summary>
        /// Set the entity this event will be invoked on.
        /// </summary>
        /// <param name="entity"></param>
        public void SetEntity(BaseEntity entity = null)
        {
            if (entity != null)
            {
                _type = NetworkEventType.Entity;
            }
            else
            {
                _type = NetworkEventType.Global;
            }

            _entity = entity;
        }

        /// <summary>
        /// Set the recipients of this event.
        /// </summary>
        /// <param name="recipients"></param>
        public void SetRecipients(List<Player> recipients)
        {
            _recipients.Clear();

            for (var i = 0; i < recipients.Count; i++)
            {
                _recipients.Add(recipients[i]);
            }
        }

        /// <summary>
        /// Add a recipient to this event.
        /// </summary>
        /// <param name="recipient"></param>
        public void AddRecipient(Player recipient)
        {
            _recipients.Add(recipient);
        }

        /// <summary>
        /// Remove a recipient from this event.
        /// </summary>
        /// <param name="recipient"></param>
        public void RemoveRecipient(Player recipient)
        {
            _recipients.Remove(recipient);
        }

        /// <summary>
        /// Ignore a recipient from receiving this event.
        /// </summary>
        /// <param name="recipient"></param>
        public void IgnoreRecipient(Player recipient)
        {
            _ignored.Add(recipient);
        }

        /// <summary>
        /// Clear all recipients of this event.
        /// </summary>
        public void ClearRecipients()
        {
            _recipients.Clear();
        }

        /// <summary>
        /// Clear the list of ignored recipients for this event.
        /// </summary>
        public void ClearIgnored()
        {
            _ignored.Clear();
        }

        /// <summary>
        /// Set the channel to send the event on. Pass in a value from your own custom enum.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="identifier"></param>
        public void SetChannel<E>(E identifier) where E : struct, IConvertible
        {
            _channel = Zapnet.Network.GetChannel(identifier);
        }

        /// <summary>
        /// Set the channel to send this event on as a hardcoded integer. It is recommended to
        /// use the overload of this method that allows you to pass in a custom enum value.
        /// </summary>
        /// <param name="channel"></param>
        public void SetChannel(int channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// Recycle this event and return it to the event pool.
        /// </summary>
        public void Recycle()
        {
            if (_recycleData)
            {
                Zapnet.Network.Recycle(_data);
            }

            if (_recycleSelf)
            {
                Zapnet.Network.Recycle(this);
            }
        }

        /// <summary>
        /// Invoke the event and call subscribers locally.
        /// </summary>
        public void Invoke()
        {
            if (_entity != null)
            {
                _entity.Call(_packetId, _data);
            }
            else
            {
                Zapnet.Network.Call(_packetId, _data);
            }
        }

        /// <summary>
        /// Send the event to its recipients.
        /// </summary>
        /// <param name="autoRecycle">Whether or not to try and automatically recycle the event and its data.</param>
        public void Send(bool autoRecycle = true)
        {
            var message = Zapnet.Network.CreateMessage();

            message.Write((byte)MessageType.Event);
            message.Write((byte)_type);
            message.Write(Zapnet.Network.ServerTick);
            message.Write(_packetId);

            if (_entity != null)
            {
                message.Write(_entity.EntityId);
            }

            _data.Write(message);

            if (Zapnet.Network.IsServer)
            {
                if (_entity != null && _recipients.Count == 0)
                {
                    if (_autoRecipients)
                    {
                        foreach (var player in _entity.ScopePlayers)
                        {
                            if (!_ignored.Contains(player) && player.IsConnected)
                            {
                                _sendList.Add(player.Connection);
                            }
                        }
                    }
                }
                else if (_recipients.Count == 0)
                {
                    if (_autoRecipients)
                    {
                        var playerList = Zapnet.Player.Players.List;

                        for (var i = 0; i < playerList.Count; i++)
                        {
                            var player = playerList[i];

                            if (!_ignored.Contains(player) && player.IsConnected)
                            {
                                _sendList.Add(player.Connection);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var player in _recipients)
                    {
                        if (!_ignored.Contains(player) && player.IsConnected)
                        {
                            _sendList.Add(player.Connection);
                        }
                    }
                }

                if (_sendList.Count > 0)
                {
                    Zapnet.Network.SendTo(message, _sendList, _method, _channel);
                }
            }
            else
            {
                Zapnet.Network.SendToServer(message, _method, _channel);
            }

            if (autoRecycle)
            {
                Recycle();
            }
        }

        /// <summary>
        /// When the object has been returned to its pool.
        /// </summary>
        public void OnRecycled()
        {
            Reset();
        }

        /// <summary>
        /// When the object has been fetched from its pool.
        /// </summary>
        public void OnFetched()
        {
            
        }
    }
}
