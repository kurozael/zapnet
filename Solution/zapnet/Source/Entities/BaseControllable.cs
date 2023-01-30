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
    /// An entity that can be controlled by a player and automatically sends and receives
    /// input event data of the provided type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireComponent(typeof(EntityState))]
    public class BaseControllable<T> : BaseEntity, IControllable where T : BaseInputEvent, new()
    {
        /// <summary>
        /// The player currently controlling this entity.
        /// </summary>
        public SyncPlayer Controller { get; } = new SyncPlayer(null);

        /// <summary>
        /// The speed at which the transform is reconciled with the server.
        /// </summary>
        [Header("Controllable Networking")]
        public float reconcileLerpSpeed = 2f;

        /// <summary>
        /// The last time the transform was reconciled.
        /// </summary>
        protected double _lastReconcileTime;

        /// <summary>
        /// A list of input data yet to be processed.
        /// </summary>
        protected List<T> _pendingInputs;

        /// <summary>
        /// The last processed sequence number.
        /// </summary>
        protected int _sequenceNumber;

        /// <summary>
        /// Assign control of this entity to the provided player.
        /// </summary>
        /// <param name="player"></param>
        public void AssignControl(Player player)
        {
            if (Zapnet.Network.IsServer)
            {
                Controller.Value = player;
                SetScope(player, true);
            }
        }

        /// <summary>
        /// Release control of this entity from whomever currently controls it.
        /// </summary>
        public void ReleaseControl()
        {
            if (Zapnet.Network.IsServer)
            {
                Controller.Value = null;
            }
        }

        /// <summary>
        /// Get whether this entity has a controller.
        /// </summary>
        /// <returns></returns>
        public bool HasController()
        {
            return (Controller.Value != null);
        }

        /// <summary>
        /// Get whether the provided player has control of this entity.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool HasControl(Player player)
        {
            return (Controller.Value == player);
        }

        /// <summary>
        /// Get whether any player has control of this entity.
        /// </summary>
        /// <returns></returns>
        public bool HasControl()
        {
            return (Controller.Value != null && Controller.Value == Zapnet.Player.LocalPlayer);
        }

        /// <summary>
        /// Invoked when this entity is created.
        /// </summary>
        public override void OnCreated()
        {
            Subscribe<T>(OnInputEvent);

            base.OnCreated();
        }

        /// <summary>
        /// Invoked when this entity is removed.
        /// </summary>
        public override void OnRemoved()
        {
            Unsubscribe<T>(OnInputEvent);

            base.OnRemoved();
        }

        /// <summary>
        /// Write all spawn data to the outgoing message.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="buffer"></param>
        public override void WriteSpawn(Player player, NetOutgoingMessage buffer)
        {
            base.WriteSpawn(player, buffer);

            if (HasControl(player))
            {
                WriteSyncVars(buffer, SyncTarget.Controller);
            }
        }

        /// <summary>
        /// Read all spawn data from the outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        public override void ReadSpawn(NetIncomingMessage buffer)
        {
            base.ReadSpawn(buffer);

            if (HasControl())
            {
                var state = GetState<ControllableState>();
                ReadSyncVars(buffer, SyncTarget.Controller, true);
                _sequenceNumber = state.lastSequenceNumber;
            }
        }

        /// <summary>
        /// Invoked when the network ticks.
        /// </summary>
        public override void Tick()
        {
            var state = GetState<ControllableState>();

            if ((Zapnet.Network.IsClient || Zapnet.Network.IsListenServer) && HasControl())
            {
                var input = Zapnet.Network.CreatePacket<T>();

                input.SequenceNumber = _sequenceNumber++;

                SendInput(input);

                if (Zapnet.Network.IsClient)
                {
                    var evnt = CreateEvent(input);
                    evnt.SetDeliveryMethod(NetDeliveryMethod.UnreliableSequenced);
                    evnt.SetChannel(NetChannel.PlayerInput);
                    evnt.RecycleData = false;
                    evnt.Data = input;
                    evnt.Send();
                }


                if (Zapnet.Network.IsListenServer)
                {
                    // Immediately update the input sequence because we're a listen server.
                    state.lastSequenceNumber = input.SequenceNumber;
                }
                else
                {
                    _pendingInputs.Add(input);
                }

                ApplyInput(input, true);

                OnInputSent(input);
            }

            if (Zapnet.Network.IsServer)
            {
                var count = _pendingInputs.Count;

                if (HasController())
                {
                    for (var i = 0; i < count; i++)
                    {
                        var input = _pendingInputs[i];

                        if (state.lastSequenceNumber < input.SequenceNumber)
                        {
                            state.lastSequenceNumber = input.SequenceNumber;
                        }

                        ApplyInput(input, true);

                        Zapnet.Network.Recycle(input);
                    }
                }

                if (count > 0)
                {
                    _pendingInputs.Clear();
                }
            }

            base.Tick();
        }

        /// <summary>
        /// Process all state information here after it has been received.
        /// </summary>
        /// <param name="isSpawning"></param>
        public override void ReadState(bool isSpawning)
        {
            var state = GetState<ControllableState>();

            if (HasControl())
            {
                var previousPosition = transform.position;
                var previousRotation = transform.rotation;

                transform.position = state.position;
                transform.rotation = state.rotation;

                ResetController();

                var j = 0;

                while (j < _pendingInputs.Count)
                {
                    var input = _pendingInputs[j];

                    if (input.SequenceNumber <= state.lastSequenceNumber)
                    {
                        Zapnet.Network.Recycle(input);
                        _pendingInputs.RemoveAt(j);
                    }
                    else
                    {
                        ApplyInput(_pendingInputs[j]);
                        j++;
                    }
                }

                var deltaTime = (float)(Zapnet.Network.ServerTime - _lastReconcileTime) * reconcileLerpSpeed;
                var distance = Vector3.Distance(transform.position, previousPosition);

                if (distance < GetTeleportDistance())
                {
                    transform.position = Vector3.Lerp(previousPosition, transform.position, deltaTime);
                }
                else
                {
                    OnTeleported();
                }

                transform.rotation = Quaternion.Slerp(previousRotation, transform.rotation, deltaTime);

                _lastReconcileTime = Zapnet.Network.ServerTime;
            }
            else
            {
                base.ReadState(isSpawning);
            }
        }

        /// <summary>
        /// Invoked when an input event is received.
        /// </summary>
        /// <param name="data"></param>
        protected virtual void OnInputEvent(T data)
        {
            var state = GetState<ControllableState>();

            if (state.lastSequenceNumber < data.SequenceNumber)
            {
                _pendingInputs.Add(data);
            }
        }

        /// <summary>
        /// Invoked when the controller is reset, this happens on the controlling client before reconciling inputs
        /// every time a new state update is received.
        /// </summary>
        protected virtual void ResetController() { }

        /// <summary>
        /// Invoked when the controller has changed.
        /// </summary>
        protected virtual void OnControllerChanged()
        {
            var lastPlayer = Controller.LastValue;
            var player = Controller.Value;

            if (lastPlayer != null)
            {
                var evnt = Zapnet.Network.CreateEvent<ControlLostEvent>();
                evnt.Data.Controllable = this;
                evnt.Data.Controller = lastPlayer;
                evnt.Invoke();
                evnt.Recycle();
            }

            if (player != null)
            {
                var evnt = Zapnet.Network.CreateEvent<ControlGainedEvent>();
                evnt.Data.Controllable = this;
                evnt.Data.Controller = player;
                evnt.Invoke();
                evnt.Recycle();
            }

            if (lastPlayer != null)
            {
                lastPlayer.Controllables.Remove(this);
                OnPlayerControlLost(lastPlayer);
            }

            if (player != null)
            {
                player.Controllables.Add(this);
                OnPlayerControlGained(player);
            }
        }

        protected override void Start()
        {
            if (Zapnet.Network.IsClient)
            {
                OnControllerChanged();
            }

            base.Start();
        }

        /// <summary>
        /// Invoked when input data has been sent to the server.
        /// </summary>
        /// <param name="input"></param>
        protected virtual void OnInputSent(T input) { }

        /// <summary>
        /// Invoked when the input data object should be modified to include all
        /// input data ready to be sent to the server.
        /// </summary>
        /// <param name="input"></param>
        protected virtual void SendInput(T input) { }

        /// <summary>
        /// Invoked when input data should be applied. Use this to update your character controller
        /// or anything else that needs to react to inputs. This is called on both the controlling client
        /// and the server.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="isFirstTime"></param>
        protected virtual void ApplyInput(T input, bool isFirstTime = false) { }

        /// <summary>
        /// Invoked when a player has lost control of the entity.
        /// </summary>
        /// <param name="player"></param>
        protected virtual void OnPlayerControlLost(Player player) { }

        /// <summary>
        /// Invoked when a player has gained control of the entity.
        /// </summary>
        /// <param name="player"></param>
        protected virtual void OnPlayerControlGained(Player player) { }

        /// <summary>
        /// Invoked when the synchronize event should be sent.
        /// </summary>
        protected override void SendSynchronizeEvent()
        {
            base.SendSynchronizeEvent();

            var totalDirtyCount = GetTotalDirtySyncVarCount(SyncTarget.Controller);

            if (totalDirtyCount > 0)
            {
                var controller = Controller.Value;

                if (controller != null)
                {
                    var evnt = CreateEvent<SynchronizeEvent>();

                    evnt.SetDeliveryMethod(NetDeliveryMethod.ReliableSequenced);
                    evnt.SetChannel(NetChannel.SyncVars);
                    evnt.AddRecipient(controller);

                    evnt.Data.Entity = this;
                    evnt.Data.Target = SyncTarget.Controller;

                    evnt.Send();
                }
            }
        }

        /// <summary>
        /// Invoked when the position and rotation should be interpolated to the values
        /// in the latest state update.
        /// </summary>
        protected override void InterpolateTransform()
        {
            if (!HasControl())
            {
                base.InterpolateTransform();
            }
        }

        protected override void Awake()
        {
            Controller.onValueChanged += OnControllerChanged;

            _pendingInputs = new List<T>();

            base.Awake();
        }
    }
}
