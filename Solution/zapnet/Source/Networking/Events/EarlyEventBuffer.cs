/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using System.Collections.Generic;
using UnityEngine;

namespace zapnet
{
    internal class EarlyEventBuffer
    {
        internal struct EarlyEventData
        {
            public NetIncomingMessage msg;
            public BaseEventData data;
        }

        internal class EarlyEventType
        {
            public List<EarlyEventData> events;
            public float staleTime;
        }

        private readonly NetClient _client;

        private float _nextCheckStale;
        private Dictionary<uint, LimitedDictionary<byte, EarlyEventType>> _buffer;

        public EarlyEventBuffer(NetClient client)
        {
            Zapnet.Entity.onEntityCreated += OnEntityCreated;

            _nextCheckStale = 0f;
            _buffer = new Dictionary<uint, LimitedDictionary<byte, EarlyEventType>>();
            _client = client;
        }

        public void ClearStaleEvents()
        {
            if (Time.time < _nextCheckStale)
            {
                return;
            }

            var removeEntities = (List<uint>)null;

            foreach (var kv in _buffer)
            {
                var entityId = kv.Key;
                var entity = Zapnet.Entity.Find(entityId);

                if (entity != null)
                {
                    if (removeEntities == null)
                    {
                        removeEntities = new List<uint>();
                    }

                    removeEntities.Add(entityId);

                    continue;
                }

                var removeEvents = (List<byte>)null;
                var packetTable = kv.Value;

                foreach (var kv2 in packetTable)
                {
                    var eventItem = kv2.Value;
                    var packetId = kv2.Key;

                    if (Time.time >= eventItem.staleTime)
                    {
                        var events = eventItem.events;

                        for (var i = 0; i < events.Count; i++)
                        {
                            _client.Recycle(events[i].msg);
                            events[i].data.Recycle();
                        }

                        if (removeEvents == null)
                        {
                            removeEvents = new List<byte>();
                        }

                        removeEvents.Add(packetId);
                    }
                }

                if (removeEvents != null)
                {
                    for (var i = 0; i < removeEvents.Count; i++)
                    {
                        packetTable.Remove(removeEvents[i]);
                    }
                }

                if (packetTable.Count == 0)
                {
                    if (removeEntities == null)
                    {
                        removeEntities = new List<uint>();
                    }

                    removeEntities.Add(entityId);
                }
            }

            if (removeEntities != null)
            {
                for (var i = 0; i < removeEntities.Count; i++)
                {
                    _buffer.Remove(removeEntities[i]);
                }
            }

            _nextCheckStale = Time.time + 0.1f;
        }

        public bool Add(uint entityId, byte packetId, BaseEventData data, NetIncomingMessage msg)
        {
            var waitSettings = data.GetEarlyEventSettings();

            if (!waitSettings.shouldWait)
            {
                return false;
            }

            if (!_buffer.TryGetValue(entityId, out var eventTable))
            {
                eventTable = _buffer[entityId] = new LimitedDictionary<byte, EarlyEventType>();

                if (waitSettings.bufferSize > 0)
                {
                    eventTable.MaxItems = waitSettings.bufferSize;
                }
                else
                {
                    eventTable.MaxItems = int.MaxValue;
                }
            }

            if (!eventTable.TryGetValue(packetId, out var packetItem))
            {
                packetItem = eventTable[packetId] = new EarlyEventType
                {
                    events = new List<EarlyEventData>()
                };
            }

            if (waitSettings.staleTime > 0f)
            {
                packetItem.staleTime = Time.time + waitSettings.staleTime;
            }

            var events = packetItem.events;

            if (waitSettings.onlyLatest)
            {
                var eventCount = events.Count;

                if (eventCount == 0 || events[eventCount - 1].data.SendTick < data.SendTick)
                {
                    events.Clear();

                    events.Add(new EarlyEventData
                    {
                        data = data,
                        msg = msg
                    });
                }
            }
            else
            {
                events.Add(new EarlyEventData
                {
                    data = data,
                    msg = msg
                });
            }

            return true;
        }

        private void OnEntityCreated(BaseEntity entity)
        {
            var entityId = entity.EntityId;

            if (!_buffer.TryGetValue(entityId, out var eventsTable))
            {
                return;
            }

            Debug.Log("[EarlyEventBuffer::OnEntityCreated] Processing the early events for " + entity.PrefabName + "#" + entityId);

            var eventItems = new List<EarlyEventData>();

            foreach (var kv in eventsTable)
            {
                var eventItem = kv.Value;
                var packetId = kv.Key;

                foreach (var item in eventItem.events)
                {
                    if (item.data.SendTick >= entity.SpawnTick)
                    {
                        eventItems.Add(item);
                    }
                }
            }

            eventItems.Sort((a, b) =>
            {
                return a.data.SendTick.CompareTo(b.data.SendTick);
            });

            for (var i = 0; i < eventItems.Count; i++)
            {
                var eventItem = eventItems[i];
                var packetId = Zapnet.Network.GetIdFromPacket(eventItem.data);

                eventItem.data.Entity = entity;

                if (!eventItem.data.Read(eventItem.msg))
                {
                    _client.Recycle(eventItem.msg);
                    eventItem.data.Recycle();
                    break;
                }

                Debug.Log("[EarlyEventBuffer::OnEntityCreated] Invoking early " + eventItem.data.GetType().Name + " for " + entity.PrefabName + "#" + entity.EntityId);

                entity.Call(packetId, eventItem.data);

                _client.Recycle(eventItem.msg);
                eventItem.data.Recycle();
            }

            eventsTable.Clear();
        }
    }
}
