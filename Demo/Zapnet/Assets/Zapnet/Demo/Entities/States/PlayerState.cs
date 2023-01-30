using Lidgren.Network;
using UnityEngine;
using zapnet;

public class PlayerState : ControllableState
{
    [HideInInspector] public BitFlags inputFlags;
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public bool isJumping;

    public override void Write(BaseEntity entity, NetOutgoingMessage message, bool isSpawning)
    {
        base.Write(entity, message, isSpawning);

        message.WriteCompressedVector3(velocity);
        message.Write(isGrounded);
        message.Write(isJumping);
        message.Write((byte)inputFlags.Value);
    }

    public override void Read(BaseEntity entity, NetIncomingMessage message, bool isSpawning)
    {
        base.Read(entity, message, isSpawning);

        velocity = message.ReadCompressedVector3();
        isGrounded = message.ReadBoolean();
        isJumping = message.ReadBoolean();
        inputFlags.Set(message.ReadByte());
    }
}
