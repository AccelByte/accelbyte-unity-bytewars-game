// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ConnectionHelper
{
    public ConnectionApprovalResult ConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response, 
        bool isServer, 
        InGameState inGameState, 
        GameModeSO[] availableInGameMode, 
        InGameMode inGameMode, 
        ServerHelper serverHelper)
    {
        ConnectionApprovalResult result = new ConnectionApprovalResult();
        GameModeSO requestedGameModeSo = null;

        InitialConnectionData initialData = GameUtility.FromByteArray<InitialConnectionData>(request.Payload);
        if (initialData == null)
        {
            RejectConnection(response, $"Connection data is invalid.");
            return null;
        }

        bool isNewPlayer = string.IsNullOrEmpty(initialData.sessionId);
        InGameMode requestedInGameMode = initialData.inGameMode;
        BytewarsLogger.Log($"Processing connection approval for clientNetworkId: {request.ClientNetworkId}. Requested game mode: {requestedInGameMode}. IsServer: {isServer}.");

        // Reject the player if the player and server have mismatch session id.
        if (!ValidateSessionId(initialData, out string sessionValidationError))
        {
            RejectConnection(response, sessionValidationError);
            return null;
        }

        // Reject if the requested game mode is invalid.
        if (!ValidateRequestedGameMode(
            requestedInGameMode, 
            inGameMode, 
            availableInGameMode, 
            out requestedGameModeSo, 
            out string gameModeValidationError)) 
        {
            RejectConnection(response, gameModeValidationError);
            return null;
        }

        // Reject if player connection is invalid.
        if (!ValidatePlayerConnection(
            request.ClientNetworkId, 
            initialData, 
            requestedGameModeSo, 
            inGameState, 
            serverHelper, 
            out Player reconnectedPlayer, 
            out string playerValidationError)) 
        {
            RejectConnection(response, playerValidationError);
            return null;
        }

        // Set connection result and approve the connection.
        result.InGameMode = requestedInGameMode;
        result.GameModeSo = requestedGameModeSo;
        result.ReconnectPlayer = reconnectedPlayer;
        response.CreatePlayerObject = true;
        response.Approved = true;
        response.Pending = false;

        return result;
    }

    private void RejectConnection(NetworkManager.ConnectionApprovalResponse response, string reason)
    {
        BytewarsLogger.Log($"Reject client connection with reason: {reason}");
        response.Reason = reason;
        response.Approved = false;
        response.Pending = false;
    }
    
    private bool ValidateSessionId(InitialConnectionData initialData, out string rejectReason) 
    {
        bool isNewPlayer = string.IsNullOrEmpty(initialData.sessionId);
        if (isNewPlayer && !string.IsNullOrEmpty(initialData.serverSessionId) && !initialData.serverSessionId.Equals(GameData.ServerSessionID))
        {
            rejectReason = $"Invalid session id between client's session id ({initialData.serverSessionId}) and server's session id ({GameData.ServerSessionID})";
            return false;
        }
        
        rejectReason = string.Empty;
        return true;
    }

    private bool ValidateRequestedGameMode(
        InGameMode requestedInGameMode, 
        InGameMode currentInGameMode, 
        GameModeSO[] availableGameMode, 
        out GameModeSO validatedGameModeSo, 
        out string rejectReason) 
    {
        validatedGameModeSo = availableGameMode.FirstOrDefault(x => x.InGameMode == requestedInGameMode);
        rejectReason = string.Empty;

        // Reject invalid game mode.
        if (validatedGameModeSo == null)
        {
            rejectReason = $"Requested game mode {requestedInGameMode.ToString()} is invalid.";
            return false;
        }

        // Reject if requested game mode is missmatched.
        if (currentInGameMode != InGameMode.None && currentInGameMode != requestedInGameMode)
        {
            rejectReason = $"Mismatch requested game mode {requestedInGameMode.ToString()} by client. The server game mode is {currentInGameMode.ToString()}";
            return false;
        }

        return true;
    }

    private bool ValidatePlayerConnection(
        ulong clientNetworkId,
        InitialConnectionData initialData,
        GameModeSO requestedGameModeSo, 
        InGameState currentInGameState, 
        ServerHelper serverHelper,
        out Player reconnectedPlayer,
        out string rejectReason) 
    {
        bool isNewPlayer = string.IsNullOrEmpty(initialData.sessionId);
        bool isGameScene = SceneManager.GetActiveScene().buildIndex == GameConstant.GameSceneBuildIndex;

        reconnectedPlayer = null;
        rejectReason = string.Empty;

        // Reject if the game is over.
        if (currentInGameState == InGameState.GameOver)
        {
            rejectReason = $"Failed to validate player connection as the game is already ended.";
            return false;
        }

        // If the game has not yet started (e.g. in match lobby), player are always treated as new player.
        if (isNewPlayer || !isGameScene)
        {
            // Create a new player state for the new player, reject if failed.
            if (serverHelper.CreateNewPlayerState(clientNetworkId, requestedGameModeSo) == null)
            {
                rejectReason = "Cannot accept new player connection. Game is already full.";
                return false;
            }
        }
        // Handle player reconnection.
        else
        {
            reconnectedPlayer = serverHelper.AddReconnectPlayerState(initialData.sessionId, clientNetworkId, requestedGameModeSo);
            if (reconnectedPlayer == null)
            {
                rejectReason = $"Cannot reconnect player. Game is already full.";
                return false;
            }
        }

        return true;
    }
}

public class ConnectionApprovalResult
{
    public InGameMode InGameMode;
    public GameModeSO GameModeSo;
    public Player ReconnectPlayer;
}