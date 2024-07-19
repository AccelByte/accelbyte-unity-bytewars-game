// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchSessionDSWrapper_Starter : MatchSessionWrapper
{
    public static Action OnCreateMatchSessionDS;
    public static Action<InGameMode, Result<SessionV2GameSession>> OnJoinMatchSessionDS;

    private void Awake()
    {
        base.Awake();
    }

    protected internal void BindMatchSessionDSEvents()
    {
        //TODO: Copy your code here
    }

    protected internal void UnbindMatchSessionDSEvents()
    {
        //TODO: Copy your code here
    }

    private void StartMatchSessionDS()
    {
        //TODO: Copy your code here
    }

    private void UnbindDSStatusChanged()
    {
        //TODO: Copy your code here
    }

    private void JoinMatchSessionDS(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        //TODO: Copy your code here
    } 

    private void OnDSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        //TODO: Copy your code here   
    }

}