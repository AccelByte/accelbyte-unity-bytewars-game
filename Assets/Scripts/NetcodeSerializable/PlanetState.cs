// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Unity.Netcode;
using UnityEngine;

public class PlanetState : INetworkSerializable
{
    public int Id; // Planet unique ID
    public string EntityId; // Owning object instance ID
    public Vector3 Position;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref EntityId);
        serializer.SerializeValue(ref Position);
    }
}
