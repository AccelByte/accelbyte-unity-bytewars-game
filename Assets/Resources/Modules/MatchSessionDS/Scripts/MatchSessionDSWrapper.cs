// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;

public class MatchSessionDSWrapper : MatchSessionWrapper
{
    public static Action OnCreateMatchSessionDS;
    public static Action<InGameMode, Result<SessionV2GameSession>> OnJoinMatchSessionDS;

    private void Awake()
    {
        base.Awake();
    }

    protected internal void BindMatchSessionDSEvents()
    {
        OnCreateMatchSessionDS += StartMatchSessionDS;
        OnJoinMatchSessionDS += JoinMatchSessionDS;
        OnCreateMatchCancelled += UnbindDSStatusChanged;
        OnLeaveSessionCompleted += UnbindDSStatusChanged;
    }

    protected internal void UnbindMatchSessionDSEvents()
    {
        OnCreateMatchSessionDS -= StartMatchSessionDS;
        OnJoinMatchSessionDS -= JoinMatchSessionDS;
        OnCreateMatchCancelled -= UnbindDSStatusChanged;
        OnLeaveSessionCompleted -= UnbindDSStatusChanged;
    }

    private void StartMatchSessionDS()
    {
        lobby.SessionV2DsStatusChanged += OnDSStatusChanged;
    }

    private void UnbindDSStatusChanged()
    {
        lobby.SessionV2DsStatusChanged -= OnDSStatusChanged;
    }

    private void JoinMatchSessionDS(InGameMode gameMode, Result<SessionV2GameSession> result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Session Configuration Template Type : {result.Value.configuration.type}");
            if (result.Value.dsInformation.status == SessionV2DsStatus.AVAILABLE)
            {
                TravelToDS(result.Value, gameMode);
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }
    } 

    private void OnDSStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (!result.IsError)
        {
            SessionV2DsStatus dsStatus = result.Value.session.dsInformation.status;
            BytewarsLogger.Log($"DS Status: {dsStatus}");
            if (dsStatus == SessionV2DsStatus.AVAILABLE)
            {
                BytewarsLogger.Log($"{SelectedGameMode}");
                lobby.SessionV2DsStatusChanged -= OnDSStatusChanged;
                TravelToDS(result.Value.session, SelectedGameMode);
                UnbindMatchSessionDSEvents();
            }
        } 
        else
        {
            BytewarsLogger.LogWarning($"Error: {result.Error.Message}");
        }
    }
}