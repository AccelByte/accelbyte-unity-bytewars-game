using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameFactory
{
    public const char PlayerInstancePrefix = 's';
    public const char PlanetInstancePrefix = 'P';

    public static CreateLevelResult CreateLevel(GameModeSO gameModeSo,
        List<GameEntityAbs> instantiatedGEs,
        Dictionary<ulong, Player> instantiatedShips,
        ObjectPooling objectPooling,
        Dictionary<int, TeamState> teamStates,
        Dictionary<ulong, PlayerState> playerStates)
    {
        CreateLevelResult result = null;
        List<LevelObject> levelObjects = new List<LevelObject>();
        float boundMinX = gameModeSo.bounds.min.x;
        float boundMaxX = gameModeSo.bounds.max.x;
        float boundMinY = gameModeSo.bounds.min.y;
        float boundMaxY = gameModeSo.bounds.max.y;
        // Debug.Log($"boundMinX:{boundMinX} boundMaxX:{boundMaxX} boundMinY:{boundMinY} boundMaxY:{boundMaxY}");
        //instantiate new empty GameObject as the parent
        for( int i = 0; i < gameModeSo.numLevelObjectsToSpawn; i++)
        {
            var randomObject = gameModeSo.objectsToSpawn[Random.Range(0, gameModeSo.objectsToSpawn.Length)];

            for (int r = 0; r < gameModeSo.numRetriesToPlaceLevelObject; r++)
            {
                Vector3 randomPosition = new Vector3(
                    Random.Range(boundMinX, boundMaxX),
                    Random.Range(boundMinY, boundMaxY),
                    0.0f
                );

                if (!GameUtility.IsTooCloseToOtherObject(instantiatedGEs, randomPosition, randomObject.GetRadius()))
                {
                    var planet = objectPooling.Get(randomObject);
                    planet.transform.position = randomPosition;
                    planet.SetId(i);
                    levelObjects.Add(new LevelObject()
                    {
                        m_prefabName = randomObject.name,
                        m_rotation = Quaternion.identity,
                        m_position = randomPosition,
                        ID = i
                    });
                    instantiatedGEs.Add(planet);
                    break;
                }
            }
        }
        var availablePosition= CreateAvailablePositions(gameModeSo, instantiatedGEs);
        var players = SpawnLocalPlayers(gameModeSo, instantiatedGEs, 
            instantiatedShips, objectPooling, levelObjects, availablePosition,
            teamStates, playerStates);
        result = new CreateLevelResult()
        {
            LevelObjects = levelObjects.ToArray(),
            AvailablePositions = availablePosition
        };
        return result;
    }

    private const float AvailablePosDistance = 1;
    private static List<Vector3> CreateAvailablePositions(GameModeSO gameModeSo, List<GameEntityAbs> instantiatedGEs)
    {
        List<Vector3> positions = new List<Vector3>();
        float boundMinX = gameModeSo.bounds.min.x;
        float boundMaxX = gameModeSo.bounds.max.x;
        float boundMinY = gameModeSo.bounds.min.y;
        float boundMaxY = gameModeSo.bounds.max.y;
        float posX = boundMinX;
        float posY = boundMinY;
        while (posY<boundMaxY)
        {
            posX = boundMinX;
            while (posX<boundMaxX)
            {
                positions.Add(new Vector3(posX,posY));
                posX++;
            }
            posY++;
        }
        List<Vector3> availablePositions = new List<Vector3>();
        foreach (var pos in positions)
        {
            if (GameUtility.IsTooCloseToOtherObject(instantiatedGEs, pos, AvailablePosDistance))
                continue;
            availablePositions.Add(pos);
        }
        //Debug.Log($" allpositionsCount:{positions.Count} availablePositionCount:{availablePositions.Count}");
        return availablePositions;
    }
    private static List<Player> SpawnLocalPlayers(GameModeSO gameModeSo, List<GameEntityAbs> instantiatedGEs, 
        Dictionary<ulong, Player> instantiatedShips, ObjectPooling objectPooling, 
        List<LevelObject> levelObjects, List<Vector3> availablePos,
        Dictionary<int, TeamState> teamStates, Dictionary<ulong, PlayerState> playerStates)
    {
        List<Player> ships = new List<Player>();
        int levelObjectCount = levelObjects.Count;
        
        if (teamStates.Count < 1)
        {
            BytewarsLogger.LogWarning("Cannot spawn player ships. Not enough team.");
            return ships;
        }

        foreach (KeyValuePair<ulong, PlayerState> playerState in playerStates)
        {
            for (int i = 0; i < gameModeSo.numRetriesToPlacePlayer; i++)
            {
                int randomIndex = Random.Range(0, availablePos.Count);
                Vector3 randomPosition = availablePos[randomIndex];
                availablePos.RemoveAt(randomIndex);

                if (!GameUtility.HasLineOfSightToOtherShip(instantiatedGEs, randomPosition, instantiatedShips))
                {
                    PlayerState tempPlayerState = playerState.Value;
                    tempPlayerState.position = randomPosition;

                    Debug.Log($"player-{tempPlayerState.playerIndex} team-{tempPlayerState.teamIndex} placed at {randomPosition} in {i} attempts");
                    
                    Player newShip = SpawnLocalPlayer(
                        gameModeSo, 
                        objectPooling, 
                        teamStates.ElementAt(tempPlayerState.teamIndex).Value.teamColour, 
                        tempPlayerState);

                    if (newShip != null)
                    {
                        ships.Add(newShip);
                        instantiatedShips.Add(tempPlayerState.clientNetworkId, newShip);
                        levelObjects.Add(new LevelObject()
                        {
                            m_prefabName = gameModeSo.playerPrefab.name,
                            m_position = randomPosition,
                            m_rotation = Quaternion.identity,
                            ID = levelObjectCount + tempPlayerState.playerIndex
                        });
                    }

                    break;
                }
            }
        }

        instantiatedGEs.AddRange(instantiatedShips.Values);
        
        return ships;
    }

    public static Player SpawnLocalPlayer(GameModeSO gameModeSo, ObjectPooling objectPooling,
        Vector4 color, PlayerState playerState)
    {
        var newShip = objectPooling.Get(gameModeSo.playerPrefab);
        Player player = newShip as Player;
        player.SetPlayerState(playerState, gameModeSo.maxInFlightMissilesPerPlayer, 
            color);
        return player;
    }

    public static InGameStateResult CreateLocalGameState(GameModeSO gameModeSo)
    {   
        var result = new InGameStateResult();
        result.m_teamStates = new Dictionary<int, TeamState>();
        result.m_playerStates = new Dictionary<ulong, PlayerState>();
        int playerIndex = 0;
        for (int a = 0; a < gameModeSo.teamCount; a++)
        {
            result.m_teamStates.Add(a, new TeamState()
            {
                teamIndex = a,
                teamColour = gameModeSo.teamColours[a]
            });
            for (int b = 0; b < gameModeSo.playerPerTeamCount; b++)
            {
                string playerName = "Player " + (playerIndex+1);
                result.m_playerStates.Add((ulong)playerIndex, new PlayerState()
                {
                    playerIndex = playerIndex,
                    playerName = playerName,
                    teamIndex = a,
                    lives = gameModeSo.playerStartLives,
                    clientNetworkId = (ulong)playerIndex,
                    playerId = GameData.CachedPlayerState.playerId
                });
                playerIndex++;
            }
        }
        return result;
    }
    
    public static Player SpawnReconnectedShip(ulong clientNetworkId, ServerHelper serverHelper, ObjectPooling objectPooling)
    {
        if (serverHelper.ConnectedPlayerStates.TryGetValue(clientNetworkId, out var playerState))
        {
            var teamColour = serverHelper.ConnectedTeamStates[playerState.teamIndex].teamColour;
            var newShip = objectPooling.Get(GameData.GameModeSo.playerPrefab);
            Player player = newShip as Player;
            player.SetPlayerState(playerState, GameData.GameModeSo.maxInFlightMissilesPerPlayer, 
                teamColour);
            return player;
        }
        return null;
    }
}

public class InGameStateResult
{
    public Dictionary<int, TeamState> m_teamStates;
    public Dictionary<ulong, PlayerState> m_playerStates;
}

public class CreateLevelResult
{
    public LevelObject[] LevelObjects;
    public List<Vector3> AvailablePositions;
}
