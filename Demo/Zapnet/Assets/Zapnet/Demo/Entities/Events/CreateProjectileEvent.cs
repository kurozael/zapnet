using Lidgren.Network;
using UnityEngine;
using zapnet;

public class CreateProjectileEvent : BaseEventData
{
    public BaseProjectile Prefab { get; set; }
    public BaseEntity Attacker { get; set; }
    public Vector3 Origin { get; set; }
    public Vector3 Spread { get; set; }
    public Quaternion Rotation { get; set; }
    public int BaseDamage { get; set; }
    public uint Tick { get; set; }

    public override void Write(NetOutgoingMessage buffer)
    {
        var networkPrefab = Prefab.GetComponent<NetworkPrefab>();

        buffer.Write(networkPrefab.uniqueName);
        buffer.Write(Attacker.EntityId);
        buffer.Write(Origin);
        buffer.Write(Spread);
        buffer.WriteCompressedQuaternion(Rotation);
        buffer.Write(BaseDamage);
        buffer.Write(Tick);
    }

    public override bool Read(NetIncomingMessage buffer)
    {
        Prefab = Zapnet.Prefab.Find<BaseProjectile>(buffer.ReadUInt16());
        Attacker = Zapnet.Entity.Find(buffer.ReadUInt32());
        Origin = buffer.ReadVector3();
        Spread = buffer.ReadVector3();
        Rotation = buffer.ReadCompressedQuaternion();
        BaseDamage = buffer.ReadInt32();
        Tick = buffer.ReadUInt32();

        return true;
    }
}
