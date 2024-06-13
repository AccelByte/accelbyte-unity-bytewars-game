// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionP2PWrapper : MatchSessionWrapper
{
    public static Action<InGameMode, Result<SessionV2GameSession>> OnCreateMatchSessionP2P;
    public static Action<InGameMode, Result<SessionV2GameSession>> OnJoinMatchSessionP2P;

    private void Awake()
    {
        base.Awake();
    }

    protected internal void BindMatchSessionP2PEvents()
    {
        OnCreateMatchSessionP2P += StartP2PHost;
        OnJoinMatchSessionP2P += StartP2PClient;
        OnCreateMatchCancelled += UnbindMatchSessionP2PEvents;
        OnLeaveSessionCompleted += UnbindMatchSessionP2PEvents;
    }

    protected internal void UnbindMatchSessionP2PEvents()
    {
        OnCreateMatchSessionP2P -= StartP2PHost;
        OnJoinMatchSessionP2P -= StartP2PClient;
        OnCreateMatchCancelled -= UnbindMatchSessionP2PEvents;
        OnLeaveSessionCompleted -= UnbindMatchSessionP2PEvents;
    }

    private void StartP2PHost(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        GameData.ServerSessionID = result.Value.id;
        P2PHelper.StartAsHost(gameMode, result.Value.id);
    }

    private void StartP2PClient(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        P2PHelper.StartAsP2PClient(result.Value.leaderId, gameMode, result.Value.id);
    } 
}