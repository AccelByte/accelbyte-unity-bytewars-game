// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameClientController : NetworkBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    public bool isGameSceneLoaded;

    private InGameState _serverGameState;
    
    public void StartOnlineGame()
    {
        if (!IsOwner)
        {
            return;
        }

        LoadSceneServerRpc();
    }

    [ServerRpc]
    private void LoadSceneServerRpc()
    {
        BytewarsLogger.Log("[Server] Load scene to start online game.");
        GameManager.Instance.StartOnlineGame();
    }

    void OnRotateShip(InputValue amount)
    {
        if (GameManager.Instance.InGameState is InGameState.GameOver)
        {
            BytewarsLogger.LogWarning("Unable to rotate ship. Game is over.");
            return;
        }

        if (!IsOwner || !IsAlive())
        {
            BytewarsLogger.LogWarning("Unable to rotate ship. Invalid game controller owner.");
            return;
        }

        float rotateValue = amount.Get<float>();
        RotateShipServerRpc(OwnerClientId, rotateValue);
        RotateShip(OwnerClientId, rotateValue);
    }

    void OnFire(InputValue amount)
    {
        if (GameManager.Instance.InGameState is InGameState.GameOver)
        {
            BytewarsLogger.LogWarning("Unable to shoot missile. Game is over.");
            return;
        }

        if (!IsOwner || !IsAlive())
        {
            BytewarsLogger.LogWarning("Unable to shoot missile. Invalid game controller owner.");
            return;
        }

        FireMissileServerRpc(OwnerClientId);
    }

    void OnChangePower(InputValue amount)
    {
        if (GameManager.Instance.InGameState is InGameState.GameOver)
        {
            BytewarsLogger.LogWarning("Unable to adjust missile power. Game is over.");
            return;
        }

        if (!IsOwner || !IsAlive())
        {
            BytewarsLogger.LogWarning("Unable to adjust missile power. Invalid game controller owner.");
            return;
        }

        float power = amount.Get<float>();
        ChangePowerServerRpc(OwnerClientId, power);
        ChangePower(OwnerClientId, power);
    }
    
    private void OnOpenPauseMenu()
    {
        if (IsOwner && GameManager.Instance.InGamePause.CanPauseGame())
        {
            GameManager.Instance.InGamePause.ToggleGamePause();
        }
    }

    private void ChangePower(ulong clientNetworkId, float amount)
    {
        var game = GameManager.Instance;
        if (game.Players.TryGetValue(clientNetworkId, out var player))
        {
            player.SetNormalisedPowerChangeSpeed(amount);
            if (amount == 0 && IsServer)
            {
                SyncPowerClientRpc(clientNetworkId, player.FirePowerLevel);
            }
        }
    }
    
    [ClientRpc]
    private void SyncPowerClientRpc(ulong clientNetworkId, float amount)
    {
        BytewarsLogger.LogWarning($"[Client] Sync missile power with amount: {amount}. Client id: {clientNetworkId}.");

        if (IsHost)
        {
            return;
        }

        if (!GameManager.Instance.Players.TryGetValue(clientNetworkId, out var player))
        {
            BytewarsLogger.LogWarning($"[Client] Unable to sync missile power. Player with client id {clientNetworkId} is not found.");
            return;
        }
        player.ChangePowerLevelDirectly(amount);
    }

    [ServerRpc]
    private void ChangePowerServerRpc(ulong clientNetworkId, float amount)
    {
        BytewarsLogger.Log($"[Server] Change power with amount: {amount}. Client id: {clientNetworkId}");
        ChangePower(clientNetworkId, amount);
    }

    [ServerRpc]
    private void RotateShipServerRpc(ulong clientNetworkId, float amount)
    {
        BytewarsLogger.Log($"[Server] Rotate ship with amount: {amount}. Client id: {clientNetworkId}");
        RotateShip(clientNetworkId, amount);
    }

    private void RotateShip(ulong clientNetworkId, float amount)
    {
        var game = GameManager.Instance;
        if (!game.Players.TryGetValue(clientNetworkId, out Player player))
        {
            return;
        }

        player.SetNormalisedRotateSpeed(amount);
        
        if (!IsServer)
        {
            return;
        }
        
        RotateShipClientRpc(clientNetworkId, amount);
        
        if (amount == 0)
        {
            SyncShipRotationClientRpc(clientNetworkId, player.transform.rotation);
        }
    }

    [ClientRpc]
    private void RotateShipClientRpc(ulong clientNetworkId, float amount)
    {
        BytewarsLogger.LogWarning($"[Client] Rotate ship with amount: {amount}. Client id: {clientNetworkId}.");

        if (IsHost)
        {
            return;
        }

        RotateShip(clientNetworkId, amount);
    }
    
    [ClientRpc]
    private void SyncShipRotationClientRpc(ulong clientNetworkId, Quaternion rotation)
    {
        BytewarsLogger.LogWarning($"[Client] Sync ship rotation: {rotation}. Client id: {clientNetworkId}.");

        if (IsHost)
        {
            return;
        }

        if (!GameManager.Instance.Players.TryGetValue(clientNetworkId, out var player))
        {
            BytewarsLogger.LogWarning($"[Client] Unable to sync ship rotation. Player with client id {clientNetworkId} is not found.");
            return;
        }
        player.transform.rotation = rotation;
    }

    [ServerRpc]
    private void FireMissileServerRpc(ulong clientNetworkId)
    {
        BytewarsLogger.Log($"[Server] Fire missile. Client id: {clientNetworkId}");

        if (GameManager.Instance.InGameState is not InGameState.Playing)
        {
            BytewarsLogger.LogWarning($"[Server] Unable to fire missile. Game is not started.");
            return;
        }

        if (!GameManager.Instance.Players.TryGetValue(clientNetworkId, out Player player))
        {
            BytewarsLogger.LogWarning($"[Server] Unable to fire missile. Player with NetID {clientNetworkId} is not found.");
            return;
        }

        if (!IsAlive(player)) 
        {
            BytewarsLogger.LogWarning($"[Server] Unable to fire missile. Player is dead.");
            return;
        }

        MissileFireState missileState = player.FireLocalMissile();
        if (missileState == null)
        {
            BytewarsLogger.LogWarning($"[Server] Unable to fire missile. Missile state is null.");
            return;    
        }

        FireMissileClientRpc(clientNetworkId, missileState);
    }

    [ClientRpc]
    private void FireMissileClientRpc(ulong clientNetworkId, MissileFireState missileFireState)
    {
        BytewarsLogger.LogWarning($"[Client] Fire missile. Client id: {clientNetworkId}.");

        if (IsHost)
        {
            return;
        }

        if (GameManager.Instance.Players.TryGetValue(clientNetworkId, out Player player) && 
            GameManager.Instance.ConnectedPlayerStates.TryGetValue(clientNetworkId, out var playerState))
        {
            player.FireMissileClient(missileFireState, playerState);
        }
        else 
        {
            BytewarsLogger.LogWarning($"[Client] Unable to fire missile. Player with NetID {clientNetworkId} is not found.");
        }
    }

    public override void OnNetworkSpawn()
    {
        playerInput.enabled = IsOwner;
        
        if (IsOwner && IsClient)
        {
            if (GameData.CachedPlayerState == null)
            {
                return;
            }

            //client send user data to server
            UpdatePlayerStateServerRpc(NetworkManager.Singleton.LocalClientId, GameData.CachedPlayerState);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerStateServerRpc(ulong clientNetworkId, PlayerState clientPlayerState)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager.ConnectedPlayerStates.TryGetValue(clientNetworkId, out PlayerState playerState))
        {
            playerState.avatarUrl = clientPlayerState.avatarUrl;
            playerState.playerName = clientPlayerState.playerName;
            playerState.playerId = clientPlayerState.playerId;

            // This function is called on network object spawn.
            // Thus, only broadcast the player state changes if the request was from client.
            if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId != clientNetworkId) 
            {
                gameManager.UpdatePlayerStatesClientRpc(
                    gameManager.ConnectedTeamStates.Values.ToArray(),
                    gameManager.ConnectedPlayerStates.Values.ToArray());
            }
        }
    }
    
    private bool IsAlive(Player player = null)
    {
        if (player != null)
        {
            return player.PlayerState.lives > 0;
        }
        
        if (GameManager.Instance.ConnectedPlayerStates.TryGetValue(OwnerClientId, out PlayerState playerState))
        {
            return playerState.lives > 0;
        }
        
        return false;
    }
}
