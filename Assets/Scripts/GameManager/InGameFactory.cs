// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InGameFactory
{
    public const char PlayerInstancePrefix = 's';
    public const char PlanetInstancePrefix = 'P';

    private const float AvailablePosDistance = 1;
    private const string DefaultLocalPlayerUserId = "00000000000000000000000000000000";

    public static LevelCreationResult CreateLevel(
        GameModeSO gameModeSo,
        List<GameEntityAbs> instantiatedGEs,
        Dictionary<ulong, Player> instantiatedShips,
        ObjectPooling objectPooling,
        Dictionary<int, TeamState> teamStates,
        Dictionary<ulong, PlayerState> playerStates)
    {
        List<LevelObject> levelObjects = new List<LevelObject>();

        float boundMinX = gameModeSo.Bounds.min.x;
        float boundMaxX = gameModeSo.Bounds.max.x;
        float boundMinY = gameModeSo.Bounds.min.y;
        float boundMaxY = gameModeSo.Bounds.max.y;

        // Spawn planets.
        for(int i = 0; i < gameModeSo.NumLevelObjectsToSpawn; i++)
        {
            GameEntityAbs objectToSpawn = gameModeSo.ObjectsToSpawn[Random.Range(0, gameModeSo.ObjectsToSpawn.Length)];

            for (int j = 0; j < gameModeSo.NumRetriesToPlaceLevelObject; j++)
            {
                Vector3 spawnPosition = new Vector3(
                    Random.Range(boundMinX, boundMaxX), 
                    Random.Range(boundMinY, boundMaxY), 
                    0.0f);

                // Check for valid position before spawn the level object. On last try, spawn the level object anyway.
                bool isLastTry = j >= gameModeSo.NumRetriesToPlaceLevelObject;
                if (isLastTry || !GameUtility.IsTooCloseToOtherObject(instantiatedGEs, spawnPosition, objectToSpawn.GetRadius()))
                {
                    GameEntityAbs gameEntity = objectPooling.Get(objectToSpawn);
                    gameEntity.transform.position = spawnPosition;
                    gameEntity.SetId(i);

                    levelObjects.Add(new LevelObject()
                    {
                        PrefabName = objectToSpawn.name,
                        Rotation = Quaternion.identity,
                        Position = spawnPosition,
                        ID = i
                    });

                    instantiatedGEs.Add(gameEntity);
                    break;
                }
            }
        }

        // Spawn players.
        List<Vector3> availablePositions = CreateAvailablePositions(gameModeSo, instantiatedGEs);
        SpawnLocalPlayers(
            gameModeSo, 
            instantiatedGEs, 
            instantiatedShips, 
            objectPooling, 
            levelObjects, 
            availablePositions,
            teamStates, 
            playerStates);

        // Return level.
        return new LevelCreationResult()
        {
            LevelObjects = levelObjects.ToArray(),
            AvailablePositions = availablePositions.ToArray()
        };
    }

    private static List<Vector3> CreateAvailablePositions(GameModeSO gameModeSo, List<GameEntityAbs> instantiatedGEs)
    {
        List<Vector3> positions = new List<Vector3>();
        
        float boundMinX = gameModeSo.Bounds.min.x;
        float boundMaxX = gameModeSo.Bounds.max.x;
        float boundMinY = gameModeSo.Bounds.min.y;
        float boundMaxY = gameModeSo.Bounds.max.y;

        float posX = boundMinX;
        float posY = boundMinY;
        while(posY < boundMaxY)
        {
            posX = boundMinX;
            while(posX < boundMaxX)
            {
                positions.Add(new Vector3(posX, posY));
                posX++;
            }
            posY++;
        }

        List<Vector3> availablePositions = new List<Vector3>();
        foreach (Vector3 pos in positions)
        {
            if (GameUtility.IsTooCloseToOtherObject(instantiatedGEs, pos, AvailablePosDistance)) 
            {
                continue;
            }

            availablePositions.Add(pos);
        }

        return availablePositions;
    }

    private static List<Player> SpawnLocalPlayers(
        GameModeSO gameModeSo, 
        List<GameEntityAbs> instantiatedGEs, 
        Dictionary<ulong, Player> instantiatedShips, 
        ObjectPooling objectPooling, 
        List<LevelObject> levelObjects, 
        List<Vector3> availablePos,
        Dictionary<int, TeamState> teamStates, 
        Dictionary<ulong, PlayerState> playerStates)
    {
        List<Player> players = new List<Player>();
        if (teamStates.Count < 1)
        {
            BytewarsLogger.LogWarning("Cannot spawn player ships. Not enough team.");
            return players;
        }

        foreach (PlayerState playerState in playerStates.Values)
        {
            Vector3 spawnPosition = GetPlayerSpawnPosition(gameModeSo, instantiatedGEs, instantiatedShips, availablePos);
            playerState.Position = spawnPosition;

            Player newPlayer = SpawnLocalPlayer(
                gameModeSo,
                objectPooling,
                teamStates[playerState.TeamIndex].teamColour,
                playerState);

            if (newPlayer != null)
            {
                players.Add(newPlayer);
                instantiatedShips.Add(playerState.ClientNetworkId, newPlayer);
                levelObjects.Add(new LevelObject()
                {
                    PrefabName = gameModeSo.PlayerPrefab.name,
                    Position = spawnPosition,
                    Rotation = Quaternion.identity,
                    ID = levelObjects.Count + playerState.PlayerIndex
                });

                BytewarsLogger.Log($"Player-{playerState.PlayerIndex} from Team-{playerState.TeamIndex} is placed at {spawnPosition}");
            }
            else 
            {
                BytewarsLogger.LogWarning($"Unable to spawn Player-{playerState.PlayerIndex} from Team-{playerState.TeamIndex}. Player instance is null.");
            }
        }

        instantiatedGEs.AddRange(instantiatedShips.Values);
        
        return players;
    }

    public static Player SpawnLocalPlayer(
        GameModeSO gameModeSo, 
        ObjectPooling objectPooling,
        Vector4 color, 
        PlayerState playerState)
    {
        GameEntityAbs newShip = objectPooling.Get(gameModeSo.PlayerPrefab);
        Player player = newShip as Player;
        player.SetPlayerState(playerState, gameModeSo.MaxInFlightMissilesPerPlayer, color);
        return player;
    }

    public static void RespawnLocalPlayer(
        Player playerToRespawn,
        GameModeSO gameModeSo,
        List<GameEntityAbs> instantiatedGEs,
        Dictionary<ulong, Player> instantiatedShips,
        List<Vector3> availablePositions)
    {
        Vector3 oldPosition = playerToRespawn.transform.position;

        // Spawn the player on a new position.
        Vector3 newPosition = GetPlayerSpawnPosition(gameModeSo, instantiatedGEs, instantiatedShips, availablePositions);
        Color teamColor = gameModeSo.TeamColours[playerToRespawn.PlayerState.TeamIndex];
        playerToRespawn.PlayerState.Position = newPosition;
        playerToRespawn.Initialize(gameModeSo.MaxInFlightMissilesPerPlayer, teamColor);

        // Add old position to available positions.
        availablePositions.Add(oldPosition);

        BytewarsLogger.Log($"Player {playerToRespawn.PlayerState.PlayerId} is respawned on the coord: {playerToRespawn.PlayerState.Position}.");

        // Broadcast to connected game clients.
        GameManager.Instance.RepositionPlayerClientRpc(
            playerToRespawn.PlayerState.ClientNetworkId,
            newPosition,
            gameModeSo.MaxInFlightMissilesPerPlayer,
            teamColor,
            playerToRespawn.transform.rotation);
    }

    public static Vector3 GetPlayerSpawnPosition(
        GameModeSO gameModeSo,
        List<GameEntityAbs> instantiatedGEs,
        Dictionary<ulong, Player> instantiatedShips,
        List<Vector3> availablePositions)
    {
        // Check for valid position to spawn the player's ship.
        int spawnPosIndex = 0;
        for (int i = 0; i < gameModeSo.NumRetriesToPlacePlayer; i++) 
        {
            spawnPosIndex = Random.Range(0, availablePositions.Count);
            if (!GameUtility.HasLineOfSightToOtherShip(instantiatedGEs, availablePositions[spawnPosIndex], instantiatedShips))
            {
                break;
            }
        }

        Vector3 spawnPosition = availablePositions[spawnPosIndex];
        availablePositions.RemoveAt(spawnPosIndex);

        return spawnPosition;
    }

    public static InGameStateResult CreateLocalGameState(GameModeSO gameModeSo)
    {
        InGameStateResult result = new InGameStateResult();

        int playerIndex = 0;
        for (int i = 0; i < gameModeSo.TeamCount; i++)
        {
            result.TeamStates.Add(i, new TeamState()
            {
                teamIndex = i,
                teamColour = gameModeSo.TeamColours[i]
            });

            for (int j = 0; j < gameModeSo.PlayerPerTeamCount; j++)
            {
                // If first player, use AccelByte user ID. Otherwise, use default local user ID.
                string playerId = playerIndex == 0 ? GameData.CachedPlayerState.PlayerId : DefaultLocalPlayerUserId;

                string playerName = $"Player {playerIndex + 1}";
                result.PlayerStates.Add((ulong)playerIndex, new PlayerState()
                {
                    PlayerIndex = playerIndex,
                    PlayerName = playerName,
                    TeamIndex = i,
                    Lives = gameModeSo.PlayerStartLives,
                    ClientNetworkId = (ulong)playerIndex,
                    PlayerId = playerId
                });
                playerIndex++;
            }
        }

        return result;
    }
    
    public static Player SpawnReconnectedShip(ulong clientNetworkId, ServerHelper serverHelper, ObjectPooling objectPooling)
    {
        Player player = null;
        if (serverHelper.ConnectedPlayerStates.TryGetValue(clientNetworkId, out var playerState))
        {
            Color teamColor = serverHelper.ConnectedTeamStates[playerState.TeamIndex].teamColour;
            GameEntityAbs newShip = objectPooling.Get(GameData.GameModeSo.PlayerPrefab);

            player = newShip as Player;
            player.SetPlayerState(playerState, GameData.GameModeSo.MaxInFlightMissilesPerPlayer, teamColor);
        }

        return player;
    }
}

public class InGameStateResult
{
    public Dictionary<int, TeamState> TeamStates = new Dictionary<int, TeamState>();
    public Dictionary<ulong, PlayerState> PlayerStates = new Dictionary<ulong, PlayerState>();
}

[System.Serializable]
public class LevelCreationResult : INetworkSerializable
{
    public LevelObject[] LevelObjects;
    public Vector3[] AvailablePositions;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LevelObjects);
        serializer.SerializeValue(ref AvailablePositions);
    }
}
