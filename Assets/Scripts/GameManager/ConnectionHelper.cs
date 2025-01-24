// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionHelper
{
    private static int ServerClaimedMaxWaitSec = 7;
    public ConnectionApprovalResult ConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response, 
        bool isServer, 
        InGameState inGameState, 
        GameModeSO[] availableInGameMode, 
        InGameMode inGameMode, 
        ServerHelper serverHelper)
    {
        ConnectionApprovalResult result = null;
        InitialConnectionData initialData = GameUtility.FromByteArray<InitialConnectionData>(request.Payload);
        
        int clientRequestedGameModeIndex = (int)initialData.inGameMode;
        BytewarsLogger.Log($"ConnectionApprovalCallback IsServer:{isServer} requested game mode:{clientRequestedGameModeIndex} clientNetworkId{request.ClientNetworkId}");

        bool isNewPlayer = string.IsNullOrEmpty(initialData.sessionId);

        // Reject the player if the player and server have mismatch session id.
        if (isNewPlayer &&
            !string.IsNullOrEmpty(initialData.serverSessionId) &&
            !initialData.serverSessionId.Equals(GameData.ServerSessionID))
        {
            string reason = $"Invalid session id between client's session id ({initialData.serverSessionId}) and server's session id ({GameData.ServerSessionID})";
            RejectConnection(response, reason);
            return null;
        }

        // Set server game mode if none.
        GameModeSO gameModeSo = availableInGameMode[clientRequestedGameModeIndex];
        if (inGameMode == InGameMode.None)
        {
            result = new ConnectionApprovalResult()
            {
                InGameMode = (InGameMode)clientRequestedGameModeIndex,
                GameModeSo = gameModeSo
            };
        }
        // Reject the player is the request game mode is different from the server.
        else
        {
            InGameMode requestedGameMode = (InGameMode)clientRequestedGameModeIndex;
            if (inGameMode != requestedGameMode)
            {
                string reason = $"Mismatch requested game mode {requestedGameMode.ToString()} by client {request.ClientNetworkId}. The available game mode is {inGameMode.ToString()}";
                RejectConnection(response, reason);
                return null;
            }
        }

        // If the game has not yet started, there is no reconnection and player are always treated as new player.
        bool isGameScene = SceneManager.GetActiveScene().buildIndex == GameConstant.GameSceneBuildIndex;
        if (isNewPlayer || !isGameScene)
        {
            // Create a new player state for the new player, reject if failed.
            if (serverHelper.CreateNewPlayerState(request.ClientNetworkId, gameModeSo) == null)
            {
                string reason = "Game is full. No avaiable team for the player.";
                RejectConnection(response, reason);
                return null;
            }
        }
        // Handle player reconnection.
        else
        {
            if (inGameState != InGameState.GameOver)
            {
                BytewarsLogger.Log($"Player sessionId : {initialData.sessionId} try to reconnect");

                Player player = serverHelper.AddReconnectPlayerState(initialData.sessionId, 
                    request.ClientNetworkId, 
                    availableInGameMode[clientRequestedGameModeIndex]);
                if (player)
                {
                    if (result == null)
                    {
                        result = new ConnectionApprovalResult()
                        {
                            reconnectPlayer = player
                        };
                    }
                    else
                    {
                        result.reconnectPlayer = player;
                    }
                    BytewarsLogger.Log($"Player reconnect success sessionId:{initialData.sessionId}");
                }
                else
                {
                    string reason = $"Failed to reconnect. Client network id {request.ClientNetworkId} is already claimed by other player in the game with session id {initialData.sessionId}";
                    RejectConnection(response, reason);
                    return result;
                }
            }
            else
            {
                string reason = $"Failed to reconnect. Game with session id {initialData.sessionId} is already over.";
                RejectConnection(response, reason);
                return result;
            }
        }

        // Approve connection
        response.CreatePlayerObject = true;
        response.Approved = true;
        response.Pending = false;
        return result;
    }

    private void RejectConnection(
        NetworkManager.ConnectionApprovalResponse response, 
        string reason)
    {
        BytewarsLogger.Log($"Reject client connection with reason: {reason}");
        response.Reason = reason;
        response.Approved = false;
        response.Pending = false;
    }
    
}

public class ConnectionApprovalResult
{
    public InGameMode InGameMode;
    public GameModeSO GameModeSo;
    public Player reconnectPlayer;
}