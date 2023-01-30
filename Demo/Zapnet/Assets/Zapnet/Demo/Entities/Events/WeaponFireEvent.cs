using Lidgren.Network;
using UnityEngine;
using zapnet;

public class WeaponFireEvent : BaseEventData
{
    public Vector3 Origin { get; set; }
    public Vector3 Target { get; set; }
    public uint FireTick { get; set; }

    public override void Write(NetOutgoingMessage buffer)
    {
        buffer.Write(Origin);
        buffer.Write(Target);
        buffer.Write(FireTick);
    }

    public override bool Read(NetIncomingMessage buffer)
    {
        Origin = buffer.ReadVector3();
        Target = buffer.ReadVector3();
        FireTick = buffer.ReadUInt32();

        return true;
    }
}
