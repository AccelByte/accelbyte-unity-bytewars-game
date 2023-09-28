using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class PlayerState : INetworkSerializable
{
    public string playerName;
    public int playerIndex;
    public int numMissilesFired;
    public float score;
    public int killCount;
    public int lives;
    public int teamIndex;
    public ulong clientNetworkId;
    public string sessionId;
    public Vector3 position;
    public string playerId;
    public string avatarUrl;
    public PlayerState()
    {
        playerName = "";
        sessionId = "";
        position = Vector3.zero;
        playerId = "";
        avatarUrl = "";
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerIndex);
        serializer.SerializeValue(ref numMissilesFired);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref killCount);
        serializer.SerializeValue(ref lives);
        serializer.SerializeValue(ref teamIndex);
        serializer.SerializeValue(ref clientNetworkId);
        serializer.SerializeValue(ref sessionId);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref avatarUrl);
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