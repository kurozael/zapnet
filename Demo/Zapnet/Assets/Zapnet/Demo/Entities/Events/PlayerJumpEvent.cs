using Lidgren.Network;
using zapnet;

public class PlayerJumpEvent : BaseInputEvent
{
    public override void Write(NetOutgoingMessage buffer)
    {
        base.Write(buffer);
    }

    public override bool Read(NetIncomingMessage buffer)
    {
        return base.Read(buffer);
    }
}
