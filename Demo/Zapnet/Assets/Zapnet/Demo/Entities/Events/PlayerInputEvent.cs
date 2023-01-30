using Lidgren.Network;
using zapnet;

public class PlayerInputEvent : BaseInputEvent
{
    public ref BitFlags InputFlags
    {
        get
        {
            return ref _inputFlags;
        }
    }

    public float Yaw { get; set; }

    private BitFlags _inputFlags;

    public override void Write(NetOutgoingMessage buffer)
    {
        buffer.Write((byte)InputFlags.Value);
        buffer.Write(Yaw);

        base.Write(buffer);
    }

    public override bool Read(NetIncomingMessage buffer)
    {
        InputFlags.Set(buffer.ReadByte());
        Yaw = buffer.ReadSingle();

        return base.Read(buffer);
    }
}
