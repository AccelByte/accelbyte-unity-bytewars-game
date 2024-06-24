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
    private MatchmakingSessionDSWrapperServer matchmakingDSWrapperServer;

#if UNITY_SERVER

    private void Start()
    {
        if (!TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials))
        {
            return;
        }

        amsWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSAMSWrapper>();
        matchmakingDSWrapperServer = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapperServer>();

        matchmakingDSWrapperServer.BackFillProposal();
        matchmakingDSWrapperServer.OnServerSessionUpdate();

        GameManager.Instance.OnDeregisterServer += DeregisterDSFromAMS;

        LoginServer();
    }

    private void LoginServer()
    {
        amsWrapper.LoginWithClientCredentials(OnLoginServerCompleted);
    }

    private void OnLoginServerCompleted(Result result)
    {
        if (!result.IsError)
        {
            amsWrapper.OnAMSConnectionOpened += RegisterDSToAMS;
            amsWrapper.OnAMSDrainSignalReceived += DeregisterDSFromAMS;
            amsWrapper.OnAMSConnectionClosed += ShutdownDS;
            amsWrapper.SubscribeAMSEvents();
        }
    }

    private void RegisterDSToAMS()
    {
        amsWrapper.SendReadyMessageToAMS();

        string dsId = amsWrapper.DedicatedServerId;
        amsWrapper.ConnectToDSHub(dsId);
        amsWrapper.SubscribeDSHubEvents();
    }

    private void DeregisterDSFromAMS() 
    {
        amsWrapper.DisconnectFromAMS();
    }

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