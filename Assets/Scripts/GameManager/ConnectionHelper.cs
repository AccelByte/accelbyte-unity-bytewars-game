using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class ConnectionHelper
{
    private static int ServerClaimedMaxWaitSec = 7;
    public async Task<ConnectionApprovalResult> ConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response, bool isServer, 
        InGameState inGameState, GameModeSO[] availableInGameMode, InGameMode inGameMode,
        ServerHelper serverHelper)
    {
        ConnectionApprovalResult result = null;
        var initialData = GameUtility.FromByteArray<InitialConnectionData>(request.Payload);
        int clientRequestedGameModeIndex = (int)initialData.inGameMode;
        Debug.Log($"ConnectionApprovalCallback IsServer:{isServer} requested game mode:{clientRequestedGameModeIndex} clientNetworkId{request.ClientNetworkId}");
        bool isNewPlayer = String.IsNullOrEmpty(initialData.sessionId);
        if (isNewPlayer && inGameState != InGameState.None)
        {
            string reason = "server claimed event is not received in time, reject connection";
            RejectConnection(response, reason);
            return null;
        }

        int serverWaitSec = 0;
        while(String.IsNullOrEmpty(GameData.ServerSessionID))
        {
            Debug.Log("waiting DS to be claimed but player already try to connect");
            await Task.Delay(1000);
            serverWaitSec++;
            if(serverWaitSec>=ServerClaimedMaxWaitSec)
            {
                var reason = $"invalid session id, client:{initialData.serverSessionId} server:{GameData.ServerSessionID}";
                RejectConnection(response, reason);
                return null;
            }
        }

        if (isNewPlayer &&
            !String.IsNullOrEmpty(initialData.serverSessionId) &&
            !initialData.serverSessionId.Equals(GameData.ServerSessionID))
        {
            var reason = $"invalid session id, client:{initialData.serverSessionId} server:{GameData.ServerSessionID}";
            RejectConnection(response, reason);
            return null;
        }

        GameModeSO gameModeSo = availableInGameMode[clientRequestedGameModeIndex];
        
        if (inGameMode==InGameMode.None)
        {
            result = new ConnectionApprovalResult()
            {
                InGameMode = (InGameMode)clientRequestedGameModeIndex,
                GameModeSo = gameModeSo
            };
        }
        else
        {
            InGameMode requestedGameMode = (InGameMode)clientRequestedGameModeIndex;
            if (inGameMode != requestedGameMode)
            {
                string reason = $"Game Mode did not match requested:{requestedGameMode} available:{inGameMode} clientNetworkId:{request.ClientNetworkId}";
                RejectConnection(response, reason);
                return null;
            }
        }

        if (isNewPlayer)
        {
            if (serverHelper.CreateNewPlayerState(request.ClientNetworkId, gameModeSo) == null)
            {
                string reason = $"Game is full, player connection is rejected.";
                RejectConnection(response, reason);
                return null;
            }
        }
        else
        {
            //handle reconnection
            if (inGameState != InGameState.GameOver)
            {
                BytewarsLogger.Log($"player sessionId : {initialData.sessionId} try to reconnect");
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
                    Debug.Log($"player reconnect success sessionId:{initialData.sessionId}");
                }
                else
                {
                    RejectConnection(response, 
                        $"failed to reconnect, clientNetworkId already claimed by another player, sessionId:{initialData.sessionId} clientNetworkId:{request.ClientNetworkId}");
                    return result;
                }
            }
            else
            {
                RejectConnection(response,
                    $"failed to reconnect game is over, sessionId:{initialData.sessionId}");
                return result;
            }
            

        }
        //TODO verify client against IAM services before approving
        //spawns player controller
        response.CreatePlayerObject = true;
        response.Approved = true;
        response.Pending = false;
        return result;
    }

    private void RejectConnection(NetworkManager.ConnectionApprovalResponse response, string reason)
    {
        response.Reason = reason;
        response.Approved = false;
        response.Pending = false;
        Debug.Log(reason);
    }
    
}

public class ConnectionApprovalResult
{
    public InGameMode InGameMode;
    public GameModeSO GameModeSo;
    public Player reconnectPlayer;
}