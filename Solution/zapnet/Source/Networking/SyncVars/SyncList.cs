/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using System;
using System.Collections.Generic;

namespace zapnet
{
    /// <summary>
    /// A byte synchronized across the network.
    /// </summary>
    public class SyncList<T> : ISyncVar where T : ISyncVar, new()
    {
        /// <summary>
        /// A callback for when an item is added to the list.
        /// </summary>
        public delegate void OnItemAdded(int index, T item);

        /// <summary>
        /// A callback for when an item is removed from the list.
        /// </summary>
        public delegate void OnItemRemoved(int index, T item);

        /// <summary>
        /// A callback for when an item is updated in the list.
        /// </summary>
        public delegate void OnItemUpdated(int index, T item);

        /// <summary>
        /// A callback for when the list has been cleared.
        /// </summary>
        public delegate void OnListCleared();

        /// <summary>
        /// A struct containing information about a synchronized list action.
        /// </summary>
        /// <seealso cref="zapnet.ISyncVar" />
        private struct SyncListAction
        {
            public uint id;
            public byte type;
            public int index;
        }

        /// <summary>
        /// [Shared] Invoked when an item is added.
        /// </summary>
        public event OnItemAdded onItemAdded;

        /// [Shared] <summary>
        /// Invoked when an item is removed.
        /// </summary>
        public event OnItemRemoved onItemRemoved;

        /// <summary>
        /// [Shared] Invoked when an item is updated.
        /// </summary>
        public event OnItemUpdated onItemUpdated;

        /// <summary>
        /// [Shared] Invoked when the list is cleared.
        /// </summary>
        public event OnListCleared onListCleared;

        private uint _actionId;
        private List<SyncListAction> _actions;
        private List<T> _list;

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <value>
        /// The item to get or set.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public T this[int index]
        {
            set
            {
                var item = _list[index];

                if (!item.Equals(value))
                {
                    var oldItem = item;

                    if (oldItem != null)
                    {
                        oldItem.SetDirtyHandler(null);
                    }

                    value.SetDirtyHandler(OnItemBecomeDirty);
                    value.IsDirty = true;

                    _list[index] = value;
                    IsDirty = true;

                    onItemUpdated?.Invoke(index, value);
                }
            }
            get
            {
                return _list[index];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncList{T}"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public SyncList(SyncTarget target = SyncTarget.All)
        {
            _actions = new List<SyncListAction>();
            _list = new List<T>();

            Metadata = new NetBuffer();
            Target = target;
        }

        /// <summary>
        /// Which kind of player the variable will be synchronized with.
        /// </summary>
        public SyncTarget Target { get; }

        /// <summary>
        /// Whether or not the synchronized variable is dirty.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Whether or not synchronization for this variable is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Get any metadata attached to this synchronized variable.
        /// </summary>
        public NetBuffer Metadata { get; }

        /// <summary>
        /// Whether or not the synchronized variable has any metadata attached.
        /// </summary>
        public bool HasMetdata
        {
            get
            {
                return (Metadata.LengthBytes > 0);
            }
        }

        /// <summary>
        /// Get the total amount of items in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>
        /// [Server] Clears all items from the list.
        /// </summary>
        public void Clear()
        {
            if (_list.Count > 0)
            {
                _list.Clear();

                onListCleared?.Invoke();

                _actionId++;

                _actions.Add(new SyncListAction
                {
                    id = _actionId,
                    type = 2
                });
            }
        }

        /// <summary>
        /// [Server] Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            if (!_list.Contains(item))
            {
                item.SetDirtyHandler(OnItemBecomeDirty);
                item.IsDirty = true;

                _list.Add(item);
                IsDirty = true;

                var index = _list.Count - 1;

                onItemAdded?.Invoke(index, item);

                _actionId++;

                _actions.Add(new SyncListAction
                {
                    id = _actionId,
                    type = 0,
                    index = index
                });
            }
        }

        /// <summary>
        /// [Server] Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove(T item)
        {
            var index = _list.IndexOf(item);

            if (index >= 0)
            {
                _list.RemoveAt(index);

                IsDirty = true;

                onItemRemoved?.Invoke(index, item);

                _actionId++;

                _actions.Add(new SyncListAction
                {
                    id = _actionId,
                    type = 1,
                    index = index
                });
            }
        }

        /// <summary>
        /// [Server] Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            if (_list.Count > index)
            {
                var item = _list[index];

                _list.RemoveAt(index);

                IsDirty = true;

                onItemRemoved?.Invoke(index, item);

                _actionId++;

                _actions.Add(new SyncListAction
                {
                    id = _actionId,
                    type = 1,
                    index = index
                });
            }
        }

        /// <summary>
        /// Clear any metadata attached to this synchronized variable.
        /// </summary>
        public void ClearMetadata()
        {
            Metadata.Clear();
        }

        /// <summary>
        /// Read the metadata buffer from the start.
        /// </summary>
        /// <returns></returns>
        public NetBuffer ReadMetadata()
        {
            if (Metadata != null)
            {
                Metadata.ResetHead();
                return Metadata;
            }

            return null;
        }

        /// <summary>
        /// Pause synchronization updates for this list.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Resume synchronization updates for this list.
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public virtual void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            var dirtyOnly = buffer.ReadBoolean();

            if (dirtyOnly)
            {
                var actions = buffer.ReadUInt16();

                for (var i = 0; i < actions; i++)
                {
                    var actionId = buffer.ReadUInt32();
                    var index = 0;
                    var type = buffer.ReadByte();

                    if (type == 1)
                    {
                        index = buffer.ReadUInt16();
                    }

                    if (_actionId < actionId)
                    {
                        if (type == 1)
                        {
                            if (_list.Count > index)
                            {
                                var item = _list[index];

                                _list.RemoveAt(index);

                                if (item != null)
                                {
                                    onItemRemoved?.Invoke(index, item);
                                }
                            }
                        }
                        else if (type == 2)
                        {
                            onListCleared?.Invoke();
                            _list.Clear();
                        }
                        else
                        {
                            _list.Add(default);
                        }

                        _actionId = actionId;
                    }
                }

                var changes = buffer.ReadUInt16();

                for (var i = 0; i < changes; i++)
                {
                    var index = buffer.ReadUInt16();
                    var added = false;
                    var item = _list[index];

                    if (item == null)
                    {
                        added = true;
                        item = _list[index] = new T();
                    }

                    _list[index].Read(buffer, false);

                    if (added)
                    {
                        onItemAdded?.Invoke(index, item);
                    }
                    else
                    {
                        onItemUpdated?.Invoke(index, item);
                    }
                }
            } 
            else
            {
                var actionId = buffer.ReadUInt32();
                var count = buffer.ReadUInt16();

                onListCleared?.Invoke();
                _list.Clear();

                for (var i = 0; i < count; i++)
                {
                    var item = new T();
                    item.Read(buffer, false);
                    _list.Add(item);

                    onItemAdded?.Invoke(i, item);
                }

                _actionId = actionId;
            }
        }

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        public virtual void Write(NetOutgoingMessage buffer, bool dirtyOnly)
        {
            var count = _list.Count;

            if (dirtyOnly)
            {
                var actions = _actions.Count;

                buffer.Write(true);
                buffer.Write((ushort)actions);

                for (var i = 0; i < actions; i++)
                {
                    var action = _actions[i];

                    buffer.Write(action.id);
                    buffer.Write(action.type);

                    if (action.type == 1)
                    {
                        buffer.Write((ushort)action.index);
                    }
                }
                
                var dirtyCount = 0;

                for (var i = 0; i < count; i++)
                {
                    if (_list[i].IsDirty)
                    {
                        dirtyCount++;
                    }
                }

                buffer.Write((ushort)dirtyCount);

                for (var i = 0; i < count; i++)
                {
                    var item = _list[i];

                    if (item.IsDirty)
                    {
                        buffer.Write((ushort)i);
                        item.Write(buffer, dirtyOnly);
                        item.IsDirty = false;
                    }
                }

                if (actions > 0)
                {
                    _actions.Clear();
                }
            }
            else
            {
                buffer.Write(false);
                buffer.Write(_actionId);
                buffer.Write((ushort)count);

                for (var i = 0; i < count; i++)
                {
                    _list[i].Write(buffer, dirtyOnly);
                }
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_list != null)
            {
                return "SyncList<" + typeof(T).Name + ">";
            }
            else
            {
                return "NULL";
            }
        }

        /// <summary>
        /// Sets a callback to be invoked when the synchronized variable becomes dirty.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void SetDirtyHandler(Action<ISyncVar> callback)
        {
            throw new NotImplementedException();
        }

        private void OnItemBecomeDirty(ISyncVar syncVar)
        {
            var casted = (T)syncVar;
            var index = _list.IndexOf(casted);

            if (index >= 0)
            {
                onItemUpdated?.Invoke(index, casted);
            }

            IsDirty = true;
        }
    }
}
