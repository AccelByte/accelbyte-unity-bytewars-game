// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CollisionHelper 
{
    public static void OnObjectHit(Player player, Missile missile,
        Dictionary<ulong, Player> players, ServerHelper serverHelper,
        InGameHUD hud, GameManager gameManager, GameModeEnum gameMode, 
        List<Vector3> availablePositions)
    {
        PlayerState owningPlayerState = missile.GetOwningPlayerState();

        // Update team scores.
        if(owningPlayerState.teamIndex != player.PlayerState.teamIndex)
        {
            float score = GameData.GameModeSo.baseKillScore + missile.GetScore();
            Player owningPlayer = players[owningPlayerState.clientNetworkId];
            owningPlayer.AddKillScore(score);
            
            BytewarsLogger.Log($"Add team {owningPlayerState.teamIndex} score by {score}.");

            PlayerState[] playerStates = serverHelper.ConnectedPlayerStates.Values.ToArray();
            hud.UpdateKillsAndScore(owningPlayerState, playerStates);
            gameManager.UpdateScoreClientRpc(owningPlayerState, playerStates);
        }

        player.OnHitByMissile();

        // Update team lives.
        int teamIndex = player.PlayerState.teamIndex;
        int affectedTeamLive = serverHelper.GetTeamLive(teamIndex);
        hud.SetLivesValue(teamIndex, affectedTeamLive);
        gameManager.UpdateLiveClientRpc(teamIndex, affectedTeamLive);
        
        if(player.PlayerState.lives <= 0)
        {
            BytewarsLogger.Log($"Player {player.PlayerState.playerId} is dead.");

            // Remove player from world and reset attributes.
            gameManager.ActiveGEs.Remove(player);
            if (gameMode is GameModeEnum.LocalMultiplayer or GameModeEnum.SinglePlayer)
            {
                BytewarsLogger.Log($"Reset local player {player.PlayerState.playerId} attribute.");
                player.Reset();

                /* Only the first local player can pause the game.
                 * Thus, if the first local player is dead, keep the player input enabled.*/
                if (player.PlayerState.playerIndex == 0)
                {
                    player.PlayerInput.enabled = true;
                }
            }
            else if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                BytewarsLogger.Log($"Reset remote player {player.PlayerState.playerId} attribute.");
                gameManager.ResetPlayerClientRpc(player.PlayerState.clientNetworkId);
                player.Reset();
            }
        }
        else
        {
            BytewarsLogger.Log($"Player {player.PlayerState.playerId} is dead. Remaining lives: {player.PlayerState.lives}. Respawning the player.");

            // Respawn player.
            bool playerPlaced = false;
            Vector3 playerUnusedPosition = player.transform.position;
            for (int i = 0; i < GameData.GameModeSo.numRetriesToPlacePlayer; i++)
            {
                int randomIndex = Random.Range(0, availablePositions.Count);
                Vector3 randomPosition = availablePositions[randomIndex];
                availablePositions.RemoveAt(randomIndex);

                if (!GameUtility.HasLineOfSightToOtherShip(gameManager.ActiveGEs, randomPosition, players))
                {
                    Color teamColor = serverHelper.ConnectedTeamStates[player.PlayerState.teamIndex].teamColour;
                    player.PlayerState.position = randomPosition;
                    player.Init(GameData.GameModeSo.maxInFlightMissilesPerPlayer, teamColor);
                    gameManager.RepositionPlayerClientRpc(
                        player.PlayerState.clientNetworkId, 
                        randomPosition, 
                        GameData.GameModeSo.maxInFlightMissilesPerPlayer, 
                        teamColor, 
                        player.transform.rotation);
                    playerPlaced = true;

                    BytewarsLogger.Log($"Player {player.PlayerState.playerId} is respawned on the coord: {player.PlayerState.position}.");
                }

                if(playerPlaced) 
                {
                    break;
                }
            }

            // Re-add unused positions.
            availablePositions.Add(playerUnusedPosition);
        }

        gameManager.CheckForGameOverCondition();
    }
}
