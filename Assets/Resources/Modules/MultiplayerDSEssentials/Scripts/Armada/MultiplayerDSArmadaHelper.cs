// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using UnityEngine;

public class MultiplayerDSArmadaHelper: MonoBehaviour
{
    private MultiplayerDSArmadaWrapper armadaWrapper;
    private MatchmakingSessionDSWrapper matchmakingDSWrapper;

#if UNITY_SERVER

    void Start()
    {
        if (TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials))
        {
            return;
        }
        
        BytewarsLogger.Log("Starting server with Armada..");
        armadaWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSArmadaWrapper>();
        matchmakingDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();
        
        matchmakingDSWrapper.MatchMakingServerClaim();
        matchmakingDSWrapper.BackFillProposal();
        GameManager.Instance.OnDeregisterServer += UnregisterServer;
        LoginServer();
    }

    #region Armada Functions

    private void LoginServer()
    {
        armadaWrapper.LoginWithClientCredentials(OnLoginServerCompleted);
    }

    public void HandleDSHubConnection()
    {
        string serverName = armadaWrapper.DedicatedServerName;
        armadaWrapper.ConnectToDSHub(serverName);
        armadaWrapper.SubscribeDSHubEvents();
    }
    
    private void UnregisterServer()
    {
        armadaWrapper.UnregisterServer(OnUnregisterServerCompleted);
    }

    #endregion

    #region Callback Functions

    private void OnLoginServerCompleted(Result result)
    {
        if (!result.IsError)
        {
            if (!TutorialModuleManager.Instance.IsModuleActive(TutorialType.MultiplayerDSEssentials))
            {
                armadaWrapper.RegisterServer(OnRegisterServerCompleted);
            }
        }
    }

    private void OnRegisterServerCompleted(Result result)
    {
        if (!result.IsError)
        {
            HandleDSHubConnection();
        }
    }

    private void OnUnregisterServerCompleted(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Application Quit");
            Application.Quit();
        }
    }

    #endregion

#endif
}