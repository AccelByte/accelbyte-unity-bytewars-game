// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionP2PWrapper_Starter : MatchSessionWrapper
{
    public static Action<InGameMode, Result<SessionV2GameSession>> OnCreateMatchSessionP2P;
    public static Action<InGameMode, Result<SessionV2GameSession>> OnJoinMatchSessionP2P;

    private void Awake()
    {
        base.Awake();
    }

    protected internal void BindMatchSessionP2PEvents()
    {
        //TODO: Copy your code here
    }

    protected internal void UnbindMatchSessionP2PEvents()
    {
        //TODO: Copy your code here
    }

    private void StartP2PHost(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        //TODO: Copy your code here
    }

    private void StartP2PClient(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        //TODO: Copy your code here
    } 
}