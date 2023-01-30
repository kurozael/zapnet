using Lidgren.Network;
using System;
using UnityEngine;
using zapnet;

public class DissonanceReceiveEvent : BaseEventData
{
    public ArraySegment<byte> Segment { get; set; }

    public override void Write(NetOutgoingMessage buffer)
    {
        Debug.Log("Wrote: " + Segment.Count);
        buffer.Write(Segment.Count);

        for (var i = 0; i < Segment.Count; i++)
        {
            buffer.Write(Segment.Array[Segment.Offset + i]);
        }
        Debug.Log("Worked!");
    }

    public override bool Read(NetIncomingMessage buffer)
    {
        var byteCount = buffer.ReadInt32();
        var byteArray = new byte[byteCount];

        for (var i = 0; i < byteCount; i++)
        {
            byteArray[i] = buffer.ReadByte();
        }

        Segment = new ArraySegment<byte>(byteArray);

        return true;
    }
}
