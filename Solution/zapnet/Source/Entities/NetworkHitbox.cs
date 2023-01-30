/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using UnityEngine;

namespace zapnet
{
    internal struct TickTransform
    {
        public Quaternion rotation;
        public Vector3 position;
    }

    /// <summary>
    /// A behaviour for a hitbox that should be rewound automatically when a raycast is made. This is useful
    /// to predict exactly where a player was actually shooting by the time the server receives the event.
    /// </summary>
    public class NetworkHitbox : MonoBehaviour
    {
        /// <summary>
        /// The center of the hitbox in local space.
        /// </summary>
        [Header("Hitbox Settings")]
        [SerializeField]
        private Vector3 _hitboxCenter = Vector3.zero;

        /// <summary>
        /// The size of the hitbox in local space.
        /// </summary>
        [SerializeField]
        private Vector3 _hitboxSize = Vector3.one;

        private LimitedDictionary<uint, TickTransform> _history;
        private TickTransform _previous;
        private BoxCollider _collider;
        private Transform _transform;
        private BaseEntity _entity;

        /// <summary>
        /// Backup the position and rotation of this network hitbox.
        /// </summary>
        public void Backup()
        {
            _previous = new TickTransform
            {
                position = _transform.position,
                rotation = _transform.rotation
            };
        }

        /// <summary>
        /// Restore the position and rotation of this network hitbox from the last backup.
        /// </summary>
        public void Restore()
        {
            _transform.position = _previous.position;
            _transform.rotation = _previous.rotation;
        }

        /// <summary>
        /// Rewind this hitbox to the provided server tick.
        /// </summary>
        /// <param name="tick"></param>
        public void Rewind(uint tick)
        {
            if (_entity)
            {
                var currentTick = Zapnet.Network.LocalTick;
                var renderTick = tick - _entity.interpolationTicks;
                var targetTick = tick;

                if (!_history.TryGetValue(renderTick, out var render))
                {
                    uint closestTick = 0;

                    foreach (var kv in _history)
                    {
                        var historyTick = kv.Key;

                        if (historyTick <= renderTick && historyTick > closestTick)
                        {
                            closestTick = historyTick;
                            renderTick = historyTick;
                            render = kv.Value;
                            break;
                        }
                    }
                }

                if (_history.TryGetValue(targetTick, out var target))
                {
                    var progress = Mathf.Clamp01((1f / _entity.interpolationTicks) * (currentTick - targetTick));

                    _transform.position = Vector3.Lerp(render.position, target.position, progress);
					_transform.rotation = Quaternion.Lerp(render.rotation, target.rotation, progress);
						
					return;
				}
            }
			
			if (_history.TryGetValue(tick, out var data))
            {
                _transform.position = data.position;
                _transform.rotation = data.rotation;
            }
        }

        private void OnEnable()
        {
            Zapnet.Entity.AddHitbox(this);
        }

        private void OnDisable()
        {
            if (!Zapnet.IsShuttingDown)
            {
                var entity = Zapnet.Entity;

                if (entity != null)
                {
                    Zapnet.Entity.RemoveHitbox(this);
                }
            }
        }

        private void Awake()
        {
            _history = new LimitedDictionary<uint, TickTransform>
            {
                MaxItems = NetSettings.TickRate
            };

            _collider = GetComponent<BoxCollider>();

            if (!_collider)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                _collider.isTrigger = true;
                _collider.center = _hitboxCenter;
                _collider.size = _hitboxSize;
            }

            var rigidbody = GetComponent<Rigidbody>();

            if (!rigidbody)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.mass = 1;
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.interpolation = RigidbodyInterpolation.None;
            }

            _transform = transform;
            _entity = GetComponentInParent<BaseEntity>();
        }

        private void FixedUpdate()
        {
            var tick = Zapnet.Network.ServerTick;

            if (_collider)
            {
                _history[tick] = new TickTransform
                {
                    position = _transform.position,
                    rotation = _transform.rotation
                };
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(_hitboxCenter), transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(Vector3.zero, _hitboxSize);
            Gizmos.DrawWireCube(Vector3.zero, _hitboxSize);
        }
    }
}
