using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using zapnet;

public class PlayerSubsystem : EntitySubsystem
{
    public SyncList<SyncByte> blends = new SyncList<SyncByte>(SyncTarget.All);
    [SerializeField]
    public List<byte> values = new List<byte>();

    private void Awake()
    {
        blends.Add(new SyncByte(12, SyncTarget.All));
        blends.onItemAdded += Blends_onItemAdded;
    }

    private void Blends_onItemAdded(int index, SyncByte item)
    {
        values.Add(item.Value);
    }
}
