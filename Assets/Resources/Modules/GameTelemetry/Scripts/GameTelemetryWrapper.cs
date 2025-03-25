// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class GameTelemetryWrapper : MonoBehaviour
{
    private GameStandardAnalyticsClientService clientGameStandardEvent;

    private void Awake()
    {
        clientGameStandardEvent = AccelByteSDK.GetClientRegistry().GetGameStandardEvents();

        GameManager.OnGameStarted += OnGameStarted;
        GameManager.OnGameEnded += OnGameOver;

        GameManager.OnPlayerEnterGame += OnPlayerEnterGame;

        GameManager.OnPlayerDie += OnPlayerDie;
        GameManager.OnMissileDestroyed += OnMissileDestroyed;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStarted -= OnGameStarted;
        GameManager.OnGameEnded -= OnGameOver;

        GameManager.OnPlayerEnterGame -= OnPlayerEnterGame;

        GameManager.OnPlayerDie -= OnPlayerDie;
        GameManager.OnMissileDestroyed -= OnMissileDestroyed;
    }

    #region Event Listeners
    private void OnGameStarted()
    {
        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Cannot send match info event. Only game server and listen server is allowed to send game standard event.");
            return;
        }

        MatchInfoId matchInfoId = new MatchInfoId(GameManager.Instance.CurrentGuid.ToString());
        MatchInfoOptionalParameters param = new MatchInfoOptionalParameters
        {
            MatchId = GameManager.Instance.IsLocalGame ? string.Empty : GameData.ServerSessionID,
            GameMode = GameTelemetryModels.GetFormattedGameMode(GameManager.Instance.InGameMode)
        };

        SendMatchInfoEvent(matchInfoId, param);
    }

    private void OnGameOver(GameManager.GameOverReason reason)
    {
        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Cannot send match ended info event. Only game server and listen server is allowed to send game standard event.");
            return;
        }

        GameManager.Instance.GetWinner(out TeamState winnerTeam, out PlayerState winnerPlayer);

        MatchInfoId matchInfoId = new MatchInfoId(GameManager.Instance.CurrentGuid.ToString());
        MatchEndReason matchEndReason = new MatchEndReason(reason.ToString());
        MatchInfoEndedOptionalParameters param = new MatchInfoEndedOptionalParameters
        {
            MatchId = GameManager.Instance.IsLocalGame ? string.Empty : GameData.ServerSessionID,
            Winner = winnerTeam != null ? $"{winnerTeam.teamIndex + 1}" : "None"
        };

        SendMatchInfoEndedEvent(matchInfoId, matchEndReason, param);
    }

    private void OnPlayerEnterGame(PlayerState playerState)
    {
        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Cannot send match info player event. Only game server and listen server is allowed to send game standard event.");
            return;
        }

        AccelByteUserId userId = new AccelByteUserId(playerState.PlayerId);
        MatchInfoId matchInfoId = new MatchInfoId(GameManager.Instance.CurrentGuid.ToString());
        MatchInfoPlayerOptionalParameters param = new MatchInfoPlayerOptionalParameters
        {
            MatchId = GameManager.Instance.IsLocalGame ? string.Empty : GameData.ServerSessionID,
            Team = $"{playerState.TeamIndex + 1}"
        };

        SendMatchInfoPlayerEvent(userId, matchInfoId, param);
    }

    private void OnPlayerDie(
        PlayerState deathPlayerState, 
        GameEntityDestroyReason destroyReason, 
        PlayerState killerPlayerState, 
        MissileState missileState)
    {
        if (deathPlayerState == null || killerPlayerState == null || missileState == null)
        {
            BytewarsLogger.LogWarning("Cannot send player death event. Parameters are not valid.");
            return;
        }

        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Cannot send player death event. Only game server and listen server is allowed to send game standard event.");
            return;
        }

        EntityType type = new EntityType(GameEntityType.Player.ToString());
        EntityDeadOptionalParameters param = new EntityDeadOptionalParameters
        {
            UserId = new AccelByteUserId(deathPlayerState.PlayerId),
            EntityId = new EntityId(deathPlayerState.EntityId),
            DeathLocation = $"{deathPlayerState.Position.x.ToString("0.000")},{deathPlayerState.Position.y.ToString("0.000")}",
            DeathType = destroyReason.ToString(),
            DeathSource = GameTelemetryModels.GetFormattedDeathSource(destroyReason, killerPlayerState, missileState, null)
        };

        SendEntityDeadEvent(type, param);
    }

    private void OnMissileDestroyed(
        PlayerState owningPlayerState,
        MissileState missileState,
        GameEntityDestroyReason destroyReason,
        PlanetState hitPlanetState,
        PlayerState hitPlayerState)
    {
        if (owningPlayerState == null || missileState == null)
        {
            BytewarsLogger.LogWarning("Cannot send missile death event. Parameters are not valid.");
            return;
        }

        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Cannot send missile death event. Only game server and listen server is allowed to send game standard event.");
            return;
        }

        EntityType type = new EntityType(GameEntityType.Missile.ToString());
        EntityDeadOptionalParameters param = new EntityDeadOptionalParameters
        {
            EntityId = new EntityId(missileState.EntityId),
            DeathLocation = $"{missileState.Position.x.ToString("0.000")},{missileState.Position.y.ToString("0.000")}",
            DeathType = destroyReason.ToString(),
            DeathSource = GameTelemetryModels.GetFormattedDeathSource(destroyReason, hitPlayerState, missileState, hitPlanetState)
        };

        SendEntityDeadEvent(type, param);
    }
    #endregion

    #region Game Standard Event Interface
    private void SendMatchInfoEvent(MatchInfoId matchInfoId, MatchInfoOptionalParameters param) 
    {
        BytewarsLogger.Log($"Send match info event with id: {matchInfoId}. Game mode: {param.GameMode}");
#if UNITY_SERVER
        // Game Standard Event (GSE) interface for server will be available on AGS 2025.3
#else
        clientGameStandardEvent.SendMatchInfoEvent(matchInfoId, param);
#endif
    }

    private void SendMatchInfoPlayerEvent(AccelByteUserId userId, MatchInfoId matchInfoId, MatchInfoPlayerOptionalParameters param)
    {
        BytewarsLogger.Log($"Send match info player event. Player user id: {userId}. Match info id: {matchInfoId}. Team: {param.Team}");
#if UNITY_SERVER
        // Game Standard Event (GSE) interface for server will be available on AGS 2025.3
#else
        clientGameStandardEvent.SendMatchInfoPlayerEvent(userId, matchInfoId, param);
#endif
    }

    private void SendMatchInfoEndedEvent(MatchInfoId matchInfoId, MatchEndReason matchEndReason, MatchInfoEndedOptionalParameters param)
    {
        BytewarsLogger.Log($"Send match info event with id: {matchInfoId}. Reason: {matchEndReason}. Winner: {param.Winner}");
#if UNITY_SERVER
        // Game Standard Event (GSE) interface for server will be available on AGS 2025.3
#else
        clientGameStandardEvent.SendMatchInfoEndedEvent(matchInfoId, matchEndReason, param);
#endif
    }

    private void SendEntityDeadEvent(EntityType type, EntityDeadOptionalParameters param) 
    {
        BytewarsLogger.Log($"Send entity dead event: {type}. Death source: {param.DeathSource}");
#if UNITY_SERVER
        // Game Standard Event (GSE) interface for server will be available on AGS 2025.3
#else
        clientGameStandardEvent.SendEntityDeadEvent(type, param);
#endif
    }
    #endregion
}
