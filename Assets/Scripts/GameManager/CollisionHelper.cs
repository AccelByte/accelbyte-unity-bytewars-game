// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CollisionHelper 
{
    public static void OnPlayerHit(
        Player player, 
        Missile missile,
        Dictionary<ulong, Player> players, 
        ServerHelper serverHelper,
        InGameHUD hud,
        GameModeEnum gameMode, 
        List<Vector3> availablePositions)
    {
        PlayerState owningPlayerState = missile.OwningPlayerState;

        // Update team scores.
        if(owningPlayerState.TeamIndex != player.PlayerState.TeamIndex)
        {
            float score = GameData.GameModeSo.BaseKillScore + missile.GetScore();
            Player owningPlayer = players[owningPlayerState.ClientNetworkId];
            owningPlayer.AddKillScore(score);
            
            BytewarsLogger.Log($"Add team {owningPlayerState.TeamIndex} score by {score}.");

            PlayerState[] playerStates = serverHelper.ConnectedPlayerStates.Values.ToArray();
            hud.UpdateKillsAndScore(owningPlayerState, playerStates);
            GameManager.Instance.UpdateScoreClientRpc(owningPlayerState, playerStates);
        }

        player.OnHitByMissile();

        // Update team lives.
        int teamIndex = player.PlayerState.TeamIndex;
        int affectedTeamLive = serverHelper.GetTeamLive(teamIndex);
        hud.SetLivesValue(teamIndex, affectedTeamLive);
        GameManager.Instance.UpdateLiveClientRpc(teamIndex, affectedTeamLive);
        
        // Check if player is totally dead.
        if(player.PlayerState.Lives <= 0)
        {
            BytewarsLogger.Log($"Player {player.PlayerState.PlayerId} is dead.");

            // Remove player from world and reset attributes.
            GameManager.Instance.ActiveGEs.Remove(player);
            if (gameMode is GameModeEnum.LocalMultiplayer or GameModeEnum.SinglePlayer)
            {
                BytewarsLogger.Log($"Reset local player {player.PlayerState.PlayerId} attribute.");
                player.Reset();

                /* Only the first local player can pause the game.
                 * Thus, if the first local player is dead, keep the player input enabled.*/
                if (player.PlayerState.PlayerIndex == 0)
                {
                    player.PlayerInput.enabled = true;
                }
            }
            else if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                BytewarsLogger.Log($"Reset remote player {player.PlayerState.PlayerId} attribute.");
                GameManager.Instance.ResetPlayerClientRpc(player.PlayerState.ClientNetworkId);
                player.Reset();
            }
        }
        // Respawn the player if it still has lives.
        else
        {
            InGameFactory.RespawnLocalPlayer(player, GameData.GameModeSo, GameManager.Instance.ActiveGEs, players, availablePositions);
        }

        GameManager.Instance.CheckForGameOverCondition();
    }
}
