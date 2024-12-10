// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class AnalyticsWrapper : MonoBehaviour
{
    private GameTelemetry clientGameTelemetry;
    private ServerGameTelemetry serverGameTelemetry;

    private HashSet<string> clientImmediateTelemetryEventList = new HashSet<string>();
    private HashSet<string> serverImmediateTelemetryEventList = new HashSet<string>();

    private void Awake()
    {
        clientGameTelemetry = AccelByteSDK.GetClientRegistry().GetApi().GetGameTelemetry();
        serverGameTelemetry = AccelByteSDK.GetServerRegistry().GetApi().GetGameTelemetry();

        GameManager.OnPlayerDie += SendPlayerDeathTelemetry;
    }

    private void OnDestroy()
    {
        GameManager.OnPlayerDie -= SendPlayerDeathTelemetry;
    }

    private void SendPlayerDeathTelemetry(PlayerState deathPlayer, PlayerState killer) 
    {
        if (deathPlayer == null || killer == null)
        {
            BytewarsLogger.LogWarning("Cannot send player's death telemetry. Either the player or killer is not valid.");
            return;
        }

        if (GameManager.Instance.IsClient && !GameManager.Instance.IsServer)
        {
            BytewarsLogger.Log("Game is running as client. Only standalone, P2P host, and dedicated server can send player's death telemetry.");
            return;
        }

        string deathTimeIso8601 = DateTime.UtcNow.ToString("o");
        bool isSuicide = (deathPlayer == killer);
        bool isFriendlyFire = (deathPlayer.teamIndex == killer.teamIndex);

        object payload = new
        {
            deathTimeStamp = deathTimeIso8601,
            deathType = isFriendlyFire ? "Self" : "Opponent",
            deathCause = isSuicide ? "Suicide" : (isFriendlyFire ? "Killed by Teammate" : "Killed by Opponent"),
            deathLocation = $"{deathPlayer.position.x.ToString("0.000")},{deathPlayer.position.y.ToString("0.000")}",
            attempsNeeded = deathPlayer.numKilledAttemptInSingleLifetime
        };

        SendTelemetry("player_Died", payload, true);
    }

    public void SendTelemetry(string eventName, object payload, bool isImmediateEvent) 
    {
        string fixedEventName = $"unity-{eventName}";

        if (GameManager.Instance.IsDedicatedServer) 
        {
            SendTelemetryServer(fixedEventName, payload, isImmediateEvent);
        }
        else 
        {
            SendTelemetryClient(fixedEventName, payload, isImmediateEvent);
        }
    }

    private void SendTelemetryClient(string eventName, object payload, bool isImmediateEvent) 
    {
        BytewarsLogger.Log($"Sending telemetry (client) with event name: {eventName}");

        if (isImmediateEvent) 
        {
            clientImmediateTelemetryEventList.Add(eventName);
        }
        else 
        {
            clientImmediateTelemetryEventList.Remove(eventName);
        }
        clientGameTelemetry.SetImmediateEventList(clientImmediateTelemetryEventList.ToList());

        TelemetryBody telemetryBody = new TelemetryBody
        {
            EventName = eventName,
            EventNamespace = AccelByteSDK.GetClientConfig().Namespace,
            Payload = payload
        };

        clientGameTelemetry.Send(telemetryBody, result =>
        {
            if (!result.IsError) 
            {
                BytewarsLogger.Log($"Success to send telemetry (client) with event name: {eventName}");
            }
            else 
            {
                BytewarsLogger.LogWarning($"Failed to send telemetry (client) with event name: {eventName}. Error {result.Error.Code}: {result.Error.Message}");
            }
        });
    }

    private void SendTelemetryServer(string eventName, object payload, bool isImmediateEvent)
    {
        BytewarsLogger.Log($"Sending telemetry (server) with event name: {eventName}");

        if (isImmediateEvent)
        {
            serverImmediateTelemetryEventList.Add(eventName);
        }
        else 
        {
            serverImmediateTelemetryEventList.Remove(eventName);
        }
        serverGameTelemetry.SetImmediateEventList(serverImmediateTelemetryEventList.ToList());

        TelemetryBody telemetryBody = new TelemetryBody
        {
            EventName = eventName,
            EventNamespace = AccelByteSDK.GetServerConfig().Namespace,
            Payload = payload
        };

        serverGameTelemetry.Send(telemetryBody, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Success to send telemetry (server) with event name: {eventName}");
            }
            else
            {
                BytewarsLogger.LogWarning($"Failed to send telemetry (server) with event name: {eventName}. Error {result.Error.Code}: {result.Error.Message}");
            }
        });
    }
}
