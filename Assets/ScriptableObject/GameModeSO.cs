// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/GameModeSO", order = 1)]
public class GameModeSO : ScriptableObject
{
    public GameEntityAbs[] ObjectsToSpawn;
    public GameEntityAbs PlayerPrefab;
    public Bounds Bounds;
    public int NumLevelObjectsToSpawn;
    public int NumRetriesToPlaceLevelObject;
    public int NumRetriesToPlacePlayer;
    public int GameDuration;
    public float BaseKillScore;
    public int PlayerStartLives;    
    public Color[] TeamColours;
    public GameModeEnum GameMode;
    public InGameMode InGameMode;
    public int PlayerPerTeamCount;
    public int TeamCount;
    public int MinimumTeamCountToPlay;
    public int MaxInFlightMissilesPerPlayer;
    public int LobbyCountdownSecond;
    public int BeforeGameCountdownSecond;
    public int BeforeShutDownCountdownSecond;
    public int GameOverShutdownCountdown;
    public int LobbyShutdownCountdown;
}
