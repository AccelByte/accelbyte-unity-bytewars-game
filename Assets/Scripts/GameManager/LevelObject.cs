// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Unity.Netcode;
using UnityEngine;

public class LevelObject : INetworkSerializable
{
    public string PrefabName = string.Empty;
    public Vector3 Position = Vector3.zero;
    public Quaternion Rotation = Quaternion.identity;
    public int ID = 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PrefabName);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref ID);
    }
}
