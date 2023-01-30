/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;
using System;

namespace zapnet
{
    /// <summary>
    /// An interface that all synchronzied variables must implement.
    /// </summary>
    public interface ISyncVar
    {
        /// <summary>
        /// Which kind of player the variable will be synchronized with.
        /// </summary>
        SyncTarget Target { get; }

        /// <summary>
        /// Whether or not the synchronized variable is dirty.
        /// </summary>
        bool IsDirty { get; set; }

        /// <summary>
        /// Whether or not synchronization updates for this variable are paused.
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Get any metadata attached to this synchronized variable.
        /// </summary>
        NetBuffer Metadata { get; }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        void Read(NetIncomingMessage buffer, bool changeSilently);

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        void Write(NetOutgoingMessage buffer, bool dirtyOnly = false);

        /// <summary>
        /// Clear any metadata attached to this synchronized variable.
        /// </summary>
        void ClearMetadata();

        /// <summary>
        /// Read the metadata buffer from the start.
        /// </summary>
        /// <returns></returns>
        NetBuffer ReadMetadata();

        /// <summary>
        /// Sets a callback to be invoked when the synchronized variable becomes dirty.
        /// </summary>
        /// <param name="callback">The callback.</param>
        void SetDirtyHandler(Action<ISyncVar> callback);
    }

    /// <summary>
    /// Represents a synchronized variable with the provided underlying type.
    /// </summary>
    /// <typeparam name="T">The underlying type of the value.</typeparam>
    public abstract class SyncVar<T> : ISyncVar
    {
        /// <summary>
        /// Invoked when the value has changed.
        /// </summary>
        public event Action onValueChanged;

        private Action<ISyncVar> _dirtyHandler;
        private T _value;

        /// <summary>
        /// Get the last value this synchronized variable held.
        /// </summary>
        public T LastValue { get; private set; }

        /// <summary>
        /// Get the current value held by this synchronized variable.
        /// </summary>
        public T Value
        {
            get => _value;
            set => SetValue(value);
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
        /// Initializes a new instance of the <see cref="SyncVar{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncVar(T value, SyncTarget target = SyncTarget.All)
        {
            Metadata = new NetBuffer();
            LastValue = value;
            Target = target;
            _value = value;
        }

        /// <summary>
        /// Sets a callback to be invoked when the synchronized variable becomes dirty.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void SetDirtyHandler(Action<ISyncVar> callback)
        {
            _dirtyHandler = callback;
        }

        /// <summary>
        /// Pause synchronization updates for this variable.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Resume synchronization updates for this variable.
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
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
        /// Set the value of this synchronized variable.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public void SetValue(T value, bool changeSilently = false)
        {
            if (_value == null || !_value.Equals(value))
            {
                if (_value == null && value == null)
                {
                    return;
                }

                LastValue = _value;
                IsDirty = true;
                _value = value;

                if (!changeSilently)
                {
                    onValueChanged?.Invoke();
                }

                _dirtyHandler?.Invoke(this);
            }
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public abstract void Read(NetIncomingMessage buffer, bool changeSilently);

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        public abstract void Write(NetOutgoingMessage buffer, bool dirtyOnly);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_value != null)
            {
                return _value.ToString();
            }
            else
            {
                return "NULL";
            }
        }
    }
}
