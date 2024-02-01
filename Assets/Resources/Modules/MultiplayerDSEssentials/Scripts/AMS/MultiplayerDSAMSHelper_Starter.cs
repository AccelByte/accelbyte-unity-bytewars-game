// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using UnityEngine;

public class MultiplayerDSAMSHelper_Starter: MonoBehaviour
{
    private MultiplayerDSAMSWrapper_Starter amsWrapper;
    private MatchmakingSessionDSWrapper matchmakingDSWrapper;
    
#if UNITY_SERVER

    private void Start()
    {
        BytewarsLogger.Log("Starting server using AMS..");
        amsWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSAMSWrapper_Starter>();
        matchmakingDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapper>();
        
        GameManager.Instance.OnDeregisterServer += delegate {};
    }
    
    // Put your code here
    
    
#endif
}