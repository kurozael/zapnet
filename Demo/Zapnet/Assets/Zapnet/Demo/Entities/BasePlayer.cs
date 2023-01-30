using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;
using System.Linq;
using System;
using zapnet;

[RequireComponent(typeof(PlayerState))]
public class BasePlayer : BaseControllable<PlayerInputEvent>, IDamageable, IProjectileTarget
{
    public SyncList<SyncString> SyncedList = new SyncList<SyncString>();
    public SyncString Name = new SyncString("");
    public SyncBool IsDead = new SyncBool(false);
    public SyncInt Health = new SyncInt(0);

    [Header("Entity Detection")]
    public float detectionRadius = 10f;
    public float releaseRadius = 20f;

    [Header("Settings")]
    public float respawnTime = 3f;
    public int maxHealth = 100;
    
    [Header("Input")]
    public float sensitivity = 1.5f;
    public CharacterMotor motor;
    public float fireInterval;

    [Header("Camera")]
    public float cameraDistance = 10f;
    public float cameraHeight = 10f;

    private Dictionary<KeyCode, InputFlag> _keyInputMap;

    private double _nextCheckScope;
    private double _nextFireTime;
    private Camera _camera;
    private float _yaw;
    
    public Vector3 CenterPosition
    {
        get
        {
            return transform.position + new Vector3(0f, 0.1f, 0f);
        }
    }
    
    public float Yaw
    {
        get => _yaw;
        set => _yaw = value;
    }
    
    public bool IsLocalPlayer
    {
        get
        {
            return (Controller.Value == Zapnet.Player.LocalPlayer);
        }
    }

    public int GetHealth()
    {
        return Health.Value;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void Kill()
    {
        if (Zapnet.Network.IsServer)
        {
            Health.Value = 0;
            IsDead.Value = true;

            Invoke("Respawn", respawnTime);
        }
    }
    
    public void Respawn()
    {
        if (Zapnet.Network.IsServer && IsDead.Value)
        {
            Health.Value = maxHealth;
            IsDead.Value = false;
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (Zapnet.Network.IsServer && !IsDead.Value)
        {
            Health.Value = Mathf.Clamp(Health.Value - damage, 0, maxHealth);
            
            if (Health.Value <= 0)
            {
                Kill();
            }
        }
    }
    
    public virtual bool OnProjectileHit(BaseProjectile projectile, NetworkRaycastHit hit)
    {
        if (!IsDead.Value)
        {
            var direction = (hit.data.point - projectile.Origin).normalized;
            var hitPoint = hit.data.point;

            if (Zapnet.Network.IsServer)
            {
                TakeDamage(projectile.Damage);
            }

            return true;
        }

        return false;
    }
    
    /// <summary>
    /// We'll use the Tick method to update scope. Essentially, server-side we can iterate through
    /// every entity and scope it for this player if in range. If you want your entities to always
    /// be scoped for every player, you can enable that on the entity prefab with Always In Scope.
    /// </summary>
    public override void Tick()
    {
        var serverTime = Zapnet.Network.ServerTime;
        var controller = Controller.Value;
        var position = transform.position;

        if (Zapnet.Network.IsServer && controller != null)
        {
            if (serverTime >= _nextCheckScope)
            {
                var entities = Zapnet.Entity.Entities.List;

                for (var i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];

                    if (entity != this && !entity.alwaysInScope)
                    {
                        var entityPosition = entity.transform.position;

                        if (MathExtra.DoSpheresOverlap(position, entityPosition, detectionRadius, entity.interestRadius))
                        {
                            entity.SetScope(controller, true);
                        }
                        else if (!MathExtra.DoSpheresOverlap(position, entityPosition, releaseRadius, entity.interestRadius))
                        {
                            entity.SetScope(controller, false);
                        }
                    }
                }

                _nextCheckScope = serverTime + 0.5f;
            }
        }
        
        if (Zapnet.Network.IsClient || Zapnet.Network.IsListenServer)
        {
            if (IsLocalPlayer)
            {
                UpdateCamera(true);
            }
        }

        base.Tick();
    }

    public override void ReadState(bool isSpawning)
    {
        var state = GetState<PlayerState>();

        base.ReadState(isSpawning);
    }

    public override void WriteSpawn(Player player, NetOutgoingMessage message)
    {
        base.WriteSpawn(player, message);
    }

    public override void ReadSpawn(NetIncomingMessage message)
    {
        base.ReadSpawn(message);
    }

    /// <summary>
    /// When the entity is created we can subscribe to the entity events we need.
    /// </summary>
    public override void OnCreated()
    {
        Subscribe<CreateProjectileEvent>(OnCreateProjectileEvent);
        Subscribe<WeaponFireEvent>(OnWeaponFireEvent);

        SyncedList.onItemRemoved += (i, v) =>
        {
            Debug.Log("Item removed @ " + i + " = " + v.Value);
        };

        SyncedList.onItemAdded += (i, v) =>
        {
            Debug.Log("Item added @ " + i + " = " + v.Value);
        };

        SyncedList.onItemUpdated += (i, v) =>
        {
            Debug.Log("Item changed @ " + i + " = " + v.LastValue + " is now " + v.Value);
        };

        base.OnCreated();
    }

    /// <summary>
    /// When the entity is removed we can unsubscribe from entity events.
    /// </summary>
    public override void OnRemoved()
    {
        Unsubscribe<CreateProjectileEvent>(OnCreateProjectileEvent);
        Unsubscribe<WeaponFireEvent>(OnWeaponFireEvent);

        base.OnRemoved();
    }

    protected virtual void SpawnProjectiles(WeaponFireEvent data)
    {
        UnityEngine.Random.InitState((int)data.FireTick);

        var rotation = Quaternion.LookRotation((data.Target - data.Origin).normalized);
        var damage = 30;
        var prefab = Zapnet.Prefab.Find<BaseProjectile>("Missile");

        for (int i = 0; i < 1; i++)
        {
            var projectile = Instantiate(prefab, data.Origin, rotation);

            projectile.SetDamage(damage);
            projectile.SetAttacker(this);
            projectile.IgnoreHitbox(GetComponentsInChildren<NetworkHitbox>());
            projectile.IgnoreCollider(GetComponentsInChildren<Collider>());
            projectile.Initialize(data.FireTick, data.Origin, rotation, Vector3.zero, Vector3.zero);
        }
    }

    protected override void OnTeleported()
    {
        if (IsLocalPlayer)
        {
            UpdateCamera(false);
        }

        base.OnTeleported();
    }

    protected override void ResetController()
    {
        var state = GetState<PlayerState>();

        motor.state.velocity = state.velocity;
        motor.state.isJumping = state.isJumping;
        motor.state.isGrounded = state.isGrounded;
    }

    protected override float GetTeleportDistance()
    {
        return GetState<PlayerState>().velocity.magnitude + 1f;
    }

    protected override void Start()
    {
        if (HasControl())
        {
            UpdateCamera();
        }

        base.Start();
    }

    protected override void Awake()
    {
        _keyInputMap = new Dictionary<KeyCode, InputFlag>()
        {
            [KeyCode.W] = InputFlag.Forward,
            [KeyCode.S] = InputFlag.Backward,
            [KeyCode.A] = InputFlag.Left,
            [KeyCode.D] = InputFlag.Right,
            [KeyCode.LeftShift] = InputFlag.Sprint,
            [KeyCode.Mouse0] = InputFlag.Fire
        };

        var network = Zapnet.Network;

        if (network && network.IsClient)
        {
            motor.enabled = false;
        }
       
        Name.onValueChanged += OnNameChanged;
        IsDead.onValueChanged += OnIsDeadChanged;

        Health.Value = maxHealth;
        
        _camera = Camera.main;

        base.Awake();
    }

    protected override void OnPlayerControlLost(Player player)
    {
        base.OnPlayerControlLost(player);
        
        if (Zapnet.Network.IsServer)
        {
            
        }

        if (player.IsLocalPlayer)
        {
            
        }

        player.SetEntity(null);
    }

    protected override void OnPlayerControlGained(Player player)
    {
        base.OnPlayerControlGained(player);

        if (Zapnet.Network.IsServer)
        {
            
        }
        else
        {
            if (player.IsLocalPlayer)
            {
                
            }
            else
            {
                
            }
        }

        player.SetEntity(this);
    }

    protected override void ApplyInput(PlayerInputEvent input, bool isFirstTime)
    {
        var inputFlags = input.InputFlags;
        var isWalking = inputFlags.Has(InputFlag.Forward | InputFlag.Backward | InputFlag.Left | InputFlag.Right);
        var isSprinting = (isWalking && inputFlags.Has(InputFlag.Sprint));
        var wasJumping = motor.state.isJumping;
        var moveDirection = Vector3.zero;

        transform.localRotation = Quaternion.Euler(0, input.Yaw, 0);

        if (inputFlags.Has(InputFlag.Forward) ^ inputFlags.Has(InputFlag.Backward))
        {
            moveDirection.z = inputFlags.Has(InputFlag.Forward) ? 1 : -1;
        }
        else if (inputFlags.Has(InputFlag.Left) ^ inputFlags.Has(InputFlag.Right))
        {
            moveDirection.x = inputFlags.Has(InputFlag.Right) ? 1 : -1;
        }

        if (moveDirection.x != 0 || moveDirection.z != 0)
        {
            moveDirection = Vector3.Normalize(Quaternion.Euler(0, input.Yaw, 0) * moveDirection);
        }

        var moveSpeed = 3f;

        if (isSprinting)
        {
            moveSpeed = 6f;
        }

        motor.movement.maxBackwardsSpeed = moveSpeed;
        motor.movement.maxForwardSpeed = moveSpeed;
        motor.movement.maxSidewaysSpeed = moveSpeed;

        var motorState = motor.Simulate(moveDirection, false);
        var state = GetState<PlayerState>();

        state.velocity = motorState.velocity;
        state.inputFlags = inputFlags;
        state.isJumping = motorState.isJumping;
        state.isGrounded = motorState.isGrounded;

        base.ApplyInput(input, isFirstTime);
    }

    protected override void SendInput(PlayerInputEvent input)
    {
        input.InputFlags.Clear();
        
        foreach (var kv in _keyInputMap)
        {
            if (Input.GetKey(kv.Key))
            {
                input.InputFlags.Add(kv.Value);
            }
        }

        input.Yaw = _yaw;
    }

    protected override void Update()
    {
        if (Zapnet.Network.IsClient || Zapnet.Network.IsListenServer)
        {
            if (IsLocalPlayer)
            {
                UpdateAimYaw();

                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    FireWeapon();
                }
            }
        }

        base.Update();
    }

    private void OnCreateProjectileEvent(CreateProjectileEvent data)
    {
        var projectile = Instantiate(data.Prefab, data.Origin, data.Rotation);

        projectile.Initialize(data.Tick, data.Origin, data.Rotation, new Vector3(), data.Spread);
        projectile.SetDamage(data.BaseDamage);
        projectile.SetAttacker(data.Attacker);
        projectile.IgnoreHitbox(GetComponentsInChildren<NetworkHitbox>());
        projectile.IgnoreCollider(GetComponentsInChildren<Collider>());
    }

    private void OnWeaponFireEvent(WeaponFireEvent data)
    {
        if (Zapnet.Network.IsServer)
        {
            var evnt = CreateEvent<WeaponFireEvent>();
            evnt.SetDeliveryMethod(NetDeliveryMethod.Unreliable);
            evnt.IgnoreRecipient(data.Sender);
            evnt.RecycleData = false;
            evnt.Data = data;
            evnt.Send();

            for (var i = 0; i < UnityEngine.Random.Range(1, 5); i++)
            {
                var value = UnityEngine.Random.Range(0, 99999).ToString();
                SyncedList.Add(new SyncString(value.ToString()));
                Debug.Log("adding: " + value);
            }

            if (UnityEngine.Random.Range(0f, 1f) >= 0.5f)
            {
                var count = SyncedList.Count;

                if (count > 1)
                {
                    var amount = UnityEngine.Random.Range(0, count - 1);

                    if (amount > 0)
                    {
                        Debug.Log("Removing at: " + amount + " = " + SyncedList[amount].Value);
                        SyncedList.RemoveAt(amount);
                    }
                }
            }

            if (UnityEngine.Random.Range(0f, 1f) >= 0.7f)
            {
                var count = SyncedList.Count;
                var amount = UnityEngine.Random.Range(0, count - 1);

                if (amount > 0)
                {
                    Debug.Log("Changing at: " + amount + ", adding 10 to " + SyncedList[amount].Value);
                    SyncedList[amount].Value += 10;
                }

                Debug.Log("Change!");
            }


            if (UnityEngine.Random.Range(0f, 1f) >= 0.8f)
            {
                var count = SyncedList.Count;
                var amount = UnityEngine.Random.Range(0, count - 1);

                if (amount > 0)
                {
                    Debug.Log("HARD CHANGING at: " + amount + ", adding 25 to " + SyncedList[amount].Value);
                    SyncedList[amount] = new SyncString(SyncedList[amount].Value + 25);
                }

                Debug.Log("HARD Change!");
            }
        }

        SpawnProjectiles(data);
    }

    private void UpdateAimYaw()
    {
        var mouseX = Input.GetAxis("Mouse X");

        if (mouseX > 0 || mouseX < 0)
        {
            var yawMovement = mouseX;
            yawMovement *= sensitivity;

            _yaw += yawMovement;
            _yaw %= 360f;
        }
    }

    private void FireWeapon()
    {
        if (Zapnet.Network.ServerTime < _nextFireTime)
        {
            return;
        }

        var evnt = CreateEvent<WeaponFireEvent>();

        evnt.Data.Origin = CenterPosition + transform.forward * 0.05f;
        evnt.Data.Target = CenterPosition + (transform.forward * 50f);
        evnt.Data.FireTick = Zapnet.Network.ServerTick;

        Debug.Log("Fire!");

        evnt.Invoke();
        evnt.Send();

        _nextFireTime = Zapnet.Network.ServerTime + fireInterval;
    }

    private void OnIsDeadChanged()
    {
        if (IsDead.Value)
        {
            Debug.Log(Name.Value + " has died!");
        }
        else
        {
            Debug.Log(Name.Value + " has respawned!");
        }
    }

    private void OnNameChanged()
    {
        if (!string.IsNullOrEmpty(Name.LastValue))
        {
            Debug.Log(Name.LastValue + " changed their name to " + Name.Value);
        }
    }

    private void UpdateCamera(bool shouldLerp = true)
    {
        var cameraTransform = _camera.transform;
        var oldPosition = cameraTransform.position;

        cameraTransform.position = transform.position + transform.forward * -cameraDistance;
        cameraTransform.position += new Vector3(0f, cameraHeight, 0f);

        if (shouldLerp)
        {
            cameraTransform.position = Vector3.Lerp(oldPosition, cameraTransform.position, Time.fixedDeltaTime * 8f);
        }

        cameraTransform.LookAt(transform);
    }
}
