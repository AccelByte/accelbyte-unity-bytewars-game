// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using UnityEngine;

public class ClientHelper
{
    private ulong clientNetworkId;
    public ulong ClientNetworkId => clientNetworkId;
    
    public void SetClientNetworkId(ulong clientNetworkId)
    {
        this.clientNetworkId = clientNetworkId;
    }

    public void PlaceObjectsOnClient(
        LevelObject[] levelObjects, 
        ulong[] playersClientIds,
        ObjectPooling objectPooling,
        Dictionary<string, GameEntityAbs> gamePrefabs,
        Dictionary<int, Planet> planets,
        Dictionary<ulong, Player> players, 
        ServerHelper serverHelper,
        List<GameEntityAbs> activeGameEntities)
    {
        int playerClientIdIndex = 0;
        foreach(LevelObject levelObject in levelObjects)
        {
            GameEntityAbs gameEntity = objectPooling.Get(gamePrefabs[levelObject.PrefabName]);
            gameEntity.transform.position = levelObject.Position;
            gameEntity.transform.rotation = levelObject.Rotation;
            gameEntity.SetId(levelObject.ID);

            // Adding game entity to the respective collection.
            if (gameEntity is Planet planet)
            {
                planets ??= new Dictionary<int, Planet>();
                planets.TryAdd(levelObject.ID, planet);
            }
            else if (gameEntity is Player player && player != null)
            {
                players ??= new Dictionary<ulong, Player>();

                if (playerClientIdIndex >= 0 && playerClientIdIndex <= playersClientIds.Length) 
                {
                    ulong clientNetworkId = playersClientIds[playerClientIdIndex];
                    PlayerState playerState = serverHelper.ConnectedPlayerStates[clientNetworkId];
                    Color teamColour = serverHelper.ConnectedTeamStates[playerState.teamIndex].teamColour;

                    player.SetPlayerState(playerState, GameData.GameModeSo.maxInFlightMissilesPerPlayer, teamColour);
                    players.TryAdd(clientNetworkId, player);

                    playerClientIdIndex++;
                }
            }

            activeGameEntities.Add(gameEntity);
        }
    }
}
