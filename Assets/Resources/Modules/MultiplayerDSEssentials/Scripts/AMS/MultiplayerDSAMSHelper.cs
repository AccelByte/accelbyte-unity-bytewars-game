// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using AccelByte.Core;
using UnityEngine;

public class MultiplayerDSAMSHelper : MonoBehaviour
{
    private MultiplayerDSAMSWrapper amsWrapper;
    private MatchmakingSessionDSWrapper matchmakingDSWrapper;

#if UNITY_SERVER

    private void Start()
    {
        if (!TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials))
        {
            return;
        }

        amsWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSAMSWrapper>();
        matchmakingDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();

        matchmakingDSWrapper.MatchMakingServerClaim();
        matchmakingDSWrapper.BackFillProposal();
        GameManager.Instance.OnDeregisterServer += ShutdownDS;

        LoginServer();
    }

    private void LoginServer()
    {
        amsWrapper.LoginWithClientCredentials(OnLoginServerCompleted);
    }

    private void HandleAMS()
    {
        amsWrapper.OnAMSConnectionOpened += RegisterDSToAMS;
        amsWrapper.OnAMSDrainReceived += ShutdownDS;
        amsWrapper.SubscribeAMSEvents();
    }

    private void RegisterDSToAMS()
    {
        BytewarsLogger.Log("[AMS] Sending ready to AMS");
        amsWrapper.SendReadyMessageToAMS();
        HandleDSHubConnection();
    }

    public void HandleDSHubConnection()
    {
        string dsId = amsWrapper.DedicatedServerId;
        BytewarsLogger.Log($"will connect to dsid {dsId}");
        amsWrapper.ConnectToDSHub(dsId);
        amsWrapper.SubscribeDSHubEvents();
    }

    #region Callback Functions

    private void OnLoginServerCompleted(Result result)
    {
        if (!result.IsError)
        {
            HandleAMS();
        }
    }

    #endregion

    private void ShutdownDS()
    {
        BytewarsLogger.Log($"Shutting down DS..");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

#endif
}