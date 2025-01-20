// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/GameModeSO", order = 1)]
public class GameModeSO : ScriptableObject
{
    public GameEntityAbs[] objectsToSpawn;
    public GameEntityAbs playerPrefab;
    public Bounds bounds;
    public int numLevelObjectsToSpawn;
    public int numRetriesToPlaceLevelObject;
    public int numRetriesToPlacePlayer;
    public int gameDuration;
    public float baseKillScore;
    public int playerStartLives;    
    public Color[] teamColours;
    public GameModeEnum gameMode;
    public int playerPerTeamCount;
    public int teamCount;
    public int minimumTeamCountToPlay;
    public int maxInFlightMissilesPerPlayer;
    public int lobbyCountdownSecond;
    public int beforeGameCountdownSecond;
    public int beforeShutDownCountdownSecond;
    public int gameOverShutdownCountdown;
    public int lobbyShutdownCountdown;
}
