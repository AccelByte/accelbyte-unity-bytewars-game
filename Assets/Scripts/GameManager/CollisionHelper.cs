using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CollisionHelper 
{
    public static void OnObjectHit(Player player, Missile missile,
        Dictionary<ulong, Player> players, ServerHelper serverHelper,
        InGameHUD hud, GameManager game, GameModeEnum gameMode, 
        List<Vector3> availablePositions)
    {
         var owningPlayerState = missile.GetOwningPlayerState();
         if( owningPlayerState.teamIndex != player.PlayerState.teamIndex )
         {
             var score = GameData.GameModeSo.baseKillScore + missile.GetScore();
             var owningPlayer = players[owningPlayerState.clientNetworkId];
             owningPlayer.AddKillScore(score);
             var playerStates = serverHelper.ConnectedPlayerStates.Values.ToArray();
             hud.UpdateKillsAndScore(owningPlayerState, playerStates);
             game.UpdateScoreClientRpc(owningPlayerState, playerStates);
         }
         player.OnHitByMissile();
         int teamIndex = player.PlayerState.teamIndex;
         int affectedTeamLive = serverHelper.GetTeamLive(teamIndex);
         hud.SetLivesValue(teamIndex, affectedTeamLive);
         game.UpdateLiveClientRpc(teamIndex, affectedTeamLive);
         if(player.PlayerState.lives <= 0)
         {
             game.ActiveGEs.Remove(player);
             if (gameMode is 
                 GameModeEnum.LocalMultiplayer or GameModeEnum.SinglePlayer)
             {
                 player.Reset();
                 //TODO workaround, player can't pause game if first player (using keyboard PlayerInput) dead
                 if (player.PlayerState.playerIndex == 0)
                 {
                     //_playerInput.enabled = true;
                 }
             }
             else
             {
                 if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                 {
                     game.ResetPlayerClientRpc(player.PlayerState.clientNetworkId);
                     player.Reset();
                 }
             }
         }
         else
         {
             bool playerPlaced = false;
             Vector3 playerUnusedPosition = player.transform.position;
             for (int i = 0; i < GameData.GameModeSo.numRetriesToPlacePlayer; i++)
             {
                 int randomIndex = Random.Range(0, availablePositions.Count);
                 var randomPosition = availablePositions[randomIndex];
                 availablePositions.RemoveAt(randomIndex);
                 if (!GameUtility.HasLineOfSightToOtherShip(game.ActiveGEs, randomPosition, players))
                 {
                     var teamColor = serverHelper.ConnectedTeamStates[player.PlayerState.teamIndex]
                         .teamColour;
                     player.PlayerState.position = randomPosition;
                     player.Init(GameData.GameModeSo.maxInFlightMissilesPerPlayer, teamColor);
                     game.RepositionPlayerClientRpc(player.PlayerState.clientNetworkId, randomPosition, 
                         GameData.GameModeSo.maxInFlightMissilesPerPlayer, teamColor, player.transform.rotation);
                     playerPlaced = true;
                 }
                 if(playerPlaced) break; 
             }
             //re-add unused positions
             availablePositions.Add(playerUnusedPosition);
         }
         game.CheckForGameOverCondition(serverHelper.IsGameOver());
    }
}
