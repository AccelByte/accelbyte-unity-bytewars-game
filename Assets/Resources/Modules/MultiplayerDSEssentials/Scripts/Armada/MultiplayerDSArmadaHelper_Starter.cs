// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using UnityEngine;

public class MultiplayerDSArmadaHelper_Starter: MonoBehaviour
{
    public event Action OnServerLoggedIn;
    
    private MultiplayerDSArmadaWrapper_Starter armadaWrapper;
    private MatchmakingSessionDSWrapperServer matchmakingDSWrapper;

#if UNITY_SERVER

    void Start()
    {
        BytewarsLogger.Log("Starting server using AMS..");
        armadaWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSArmadaWrapper_Starter>();
        matchmakingDSWrapper = TutorialModuleManager.Instance.GetModuleClass<MatchmakingSessionDSWrapperServer>();
            
        // Put your code here
        
    }
    
#endif
}