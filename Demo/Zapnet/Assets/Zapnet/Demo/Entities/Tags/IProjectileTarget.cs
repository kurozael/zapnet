using UnityEngine;
using zapnet;

public interface IProjectileTarget
{
    bool OnProjectileHit(BaseProjectile projectile, NetworkRaycastHit hitbox);
}
