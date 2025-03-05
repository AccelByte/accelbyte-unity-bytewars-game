// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Unity.Netcode;
using UnityEngine;

public class MissileState : INetworkSerializable
{
    public int Id; // Missile unique ID
    public string EntityId; // Owning object instance ID
    public Vector3 SpawnPosition;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Color Color;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref EntityId);
        serializer.SerializeValue(ref SpawnPosition);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref Color);
    }
}
