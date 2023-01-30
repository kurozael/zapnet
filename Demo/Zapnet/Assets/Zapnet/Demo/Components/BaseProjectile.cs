using System.Collections.Generic;
using UnityEngine;
using zapnet;

[RequireComponent(typeof(NetworkPrefab))]
public class BaseProjectile : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask layerMask = Physics.DefaultRaycastLayers;
	public float moveSpeed = 5f;
	public int lifeTime = 2;

    private int _damage;
	private BaseEntity _attacker;
	private uint _spawnTick;
	private uint _killTick;
	private uint _currentTick;
	private Vector3 _origin;
	private Quaternion _rotation;
	private Vector3 _velocity;
    private float _moveSpeed;
	private HashSet<Collider> _ignoreColliders;
	private HashSet<NetworkHitbox> _ignoreHitboxes;

    public BaseEntity Attacker => _attacker;
    public Vector3 Origin => _origin;
    public int Damage => _damage;

    public void Initialize(uint spawnTick, Vector3 origin, Quaternion rotation, Vector3 direction, Vector3 spread)
    {
        transform.rotation = rotation;
        transform.position = origin;

        _velocity = (transform.forward * _moveSpeed) + direction + spread;
        _spawnTick = spawnTick;
        _currentTick = spawnTick;
        _killTick = spawnTick + (uint)(NetSettings.TickRate * lifeTime);
        _rotation = rotation;
        _origin = origin;

        IgnoreHitbox(GetComponentsInChildren<NetworkHitbox>());
        IgnoreCollider(GetComponentsInChildren<Collider>());

        Zapnet.Network.onTick += OnTick;
    }

    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = speed;
    }

	public void SetAttacker(BaseEntity attacker)
	{
		_attacker = attacker;
	}

	public void SetDamage(int damage)
	{
		_damage = damage;
	}
	
	public void IgnoreHitbox(NetworkHitbox[] hitboxes)
	{
		for (int i = 0; i < hitboxes.Length; i++)
		{
			_ignoreHitboxes.Add(hitboxes[i]);
		}
	}

    public void IgnoreCollider(Collider[] colliders)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            _ignoreColliders.Add(colliders[i]);
        }
    }

    protected virtual void Awake()
    {
        _ignoreHitboxes = new HashSet<NetworkHitbox>();
        _ignoreColliders = new HashSet<Collider>();
        _moveSpeed = moveSpeed;
    }

    protected virtual void OnDestroy()
    {
        var network = Zapnet.Network;

        if (network)
        {
            network.onTick -= OnTick;
        }
    }

    private bool OnHit(GameObject go, NetworkRaycastHit hit)
	{
		var damageable = go.GetComponent<IProjectileTarget>();

		if (damageable == null)
		{
			damageable = go.GetComponentInParent<IProjectileTarget>();
		}

		if (damageable != null)
		{
			return damageable.OnProjectileHit(this, hit);
		}

        return true;
	}

	private void ResolveCollisions(uint tick, float fixedDeltaTime)
	{
	 	var origin = GetPositionAtTick(tick);
	 	var distance = (_velocity * fixedDeltaTime).magnitude;
	 	var ray = new Ray(origin, _velocity.normalized);
	 	var hit = Zapnet.Entity.Raycast(ray, tick, distance * 2f, layerMask);

        if (hit.gameObject && hit.data.distance <= distance)
        {
            if (hit.hitbox)
            {
                if (!_ignoreHitboxes.Contains(hit.hitbox))
                {
                    if (OnHit(hit.gameObject, hit))
                    {
                        DestroyAndHide();
                        return;
                    }
                }
            }
            else if (!_ignoreColliders.Contains(hit.data.collider))
            {
                if (OnHit(hit.gameObject, hit))
                {
                    DestroyAndHide();
                    return;
                }
            }
        }
	}

	private void DestroyAndHide()
	{
        Destroy(gameObject);
	}
	
	private Vector3 GetPositionAtTick(uint tick)
	{
        var fixedDeltaTime = Zapnet.Network.FixedDeltaTime;
		var totalDelta = tick - _spawnTick;

        return _origin + (_velocity * fixedDeltaTime * totalDelta);
	}

    private bool ShouldFastForward()
    {
        var serverTick = Zapnet.Network.ServerTick;
        var allowance = _currentTick + (Zapnet.Network.RoundtripTickTime * 2);

        return (serverTick > allowance);
    }
	
	private void OnTick()
	{
        var serverTick = Zapnet.Network.ServerTick;

        if (Zapnet.Network.IsServer || ShouldFastForward())
        {
            for (; _currentTick < serverTick; _currentTick++)
            {
                ResolveCollisions(_currentTick, Zapnet.Network.FixedDeltaTime);
            }
        }
        else
        {
            ResolveCollisions(_currentTick, Zapnet.Network.FixedDeltaTime);
            _currentTick++;
        }

        if (_currentTick > _killTick)
        {
            DestroyAndHide();
            return;
        }

        transform.position = GetPositionAtTick(_currentTick);
    }
}