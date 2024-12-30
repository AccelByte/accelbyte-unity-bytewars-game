// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class PlayerState : INetworkSerializable
{
    public string playerName = string.Empty;
    public int playerIndex = -1;
    public int numMissilesFired = 0;
    public float score = 0;
    public int killCount = 0;
    public int numKilledAttemptInSingleLifetime = 0;
    public int lives = 0;
    public int teamIndex = -1;
    public ulong clientNetworkId = 0;
    public string sessionId = string.Empty;
    public Vector3 position = Vector3.zero;
    public string playerId = string.Empty;
    public string avatarUrl = string.Empty;
    public string platformId = string.Empty;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Make sure there is no null attributes.
        playerName = string.IsNullOrEmpty(playerName) ? string.Empty : playerName;
        sessionId = string.IsNullOrEmpty(sessionId) ? string.Empty : sessionId;
        playerId = string.IsNullOrEmpty(playerId) ? string.Empty : playerId;
        avatarUrl = string.IsNullOrEmpty(avatarUrl) ? string.Empty : avatarUrl;
        platformId = string.IsNullOrEmpty(platformId) ? string.Empty : platformId;

        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerIndex);
        serializer.SerializeValue(ref numMissilesFired);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref killCount);
        serializer.SerializeValue(ref numKilledAttemptInSingleLifetime);
        serializer.SerializeValue(ref lives);
        serializer.SerializeValue(ref teamIndex);
        serializer.SerializeValue(ref clientNetworkId);
        serializer.SerializeValue(ref sessionId);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref avatarUrl);
        serializer.SerializeValue(ref platformId);
    }
    
    public string GetPlayerName()
    {
        if (String.IsNullOrEmpty(playerName))
        {
            return "ByteWarrior " + clientNetworkId;
        }
        return playerName;
    }
}