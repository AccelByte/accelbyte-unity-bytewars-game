using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class TeamState : INetworkSerializable
{
    public Color teamColour;
    public int teamIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref teamColour);
        serializer.SerializeValue(ref teamIndex);
    }
}