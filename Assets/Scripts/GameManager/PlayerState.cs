// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class PlayerState : INetworkSerializable
{
    public int PlayerIndex = -1; // Player unique ID
    public int TeamIndex = -1; // Player's team ID
    public ulong ClientNetworkId = 0; // Network object ID
    public string EntityId = string.Empty; // Owning object instance ID
    
    public float Score = 0; // Player's score
    public int KillCount = 0; // Number of opponents killed
    public int Lives = 0; // Number of player lives
    public int NumMissilesFired = 0; // Number of missiles fired by the player
    public int NumKilledAttemptInSingleLifetime = 0; // Number of attempt player is about to get killed
    public Vector3 Position = Vector3.zero; // Player ship position

    public string PlayerId = string.Empty; // Cached AccelByte user ID
    public string PlayerName = string.Empty; // Default player name or AccelByte user's display name if any
    public string SessionId = string.Empty; // Cached AccelByte's session ID
    public string PlatformId = string.Empty; // Cached AccelByte user's platform
    public string AvatarUrl = string.Empty; // Cached AccelByte user's avatar URL

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Make sure there is no null string during serialization.
        EntityId = string.IsNullOrEmpty(EntityId) ? string.Empty : EntityId;
        PlayerName = string.IsNullOrEmpty(PlayerName) ? string.Empty : PlayerName;
        PlayerId = string.IsNullOrEmpty(PlayerId) ? string.Empty : PlayerId;
        SessionId = string.IsNullOrEmpty(SessionId) ? string.Empty : SessionId;
        PlatformId = string.IsNullOrEmpty(PlatformId) ? string.Empty : PlatformId;
        AvatarUrl = string.IsNullOrEmpty(AvatarUrl) ? string.Empty : AvatarUrl;

        serializer.SerializeValue(ref EntityId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref PlayerIndex);
        serializer.SerializeValue(ref NumMissilesFired);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref KillCount);
        serializer.SerializeValue(ref NumKilledAttemptInSingleLifetime);
        serializer.SerializeValue(ref Lives);
        serializer.SerializeValue(ref TeamIndex);
        serializer.SerializeValue(ref ClientNetworkId);
        serializer.SerializeValue(ref SessionId);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref AvatarUrl);
        serializer.SerializeValue(ref PlatformId);
    }

    public string GetPlayerName()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            return PlayerName;
        }

        // Return "Player {index}", e.g., "Player 1".
        if (string.IsNullOrEmpty(PlayerId) || GameManager.Instance.IsLocalGame)
        {
            return $"Player {PlayerIndex + 1}";
        }

        // Return the first five characters of the AccelByte user ID.
        return $"Player-{PlayerId[..5]}";
    }
}