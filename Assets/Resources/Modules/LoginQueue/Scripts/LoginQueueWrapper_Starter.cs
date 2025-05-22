// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading;
using UnityEngine;
using AccelByte.Models;

public class LoginQueueWrapper_Starter : MonoBehaviour
{
    private AuthEssentialsWrapper authEssentialsWrapper;
    private SinglePlatformAuthWrapper singlePlatformAuthWrapper;
    private AuthEssentialsWrapper_Starter authEssentialsWrapper_Starter;
    private SinglePlatformAuthWrapper_Starter singlePlatformAuthWrapper_Starter;

    // TODO: place your variable declarations here
    
    private void Start()
    {
        authEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        singlePlatformAuthWrapper = TutorialModuleManager.Instance.GetModuleClass<SinglePlatformAuthWrapper>();
        authEssentialsWrapper_Starter = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper_Starter>();
        singlePlatformAuthWrapper_Starter = TutorialModuleManager.Instance.GetModuleClass<SinglePlatformAuthWrapper_Starter>();

        // TODO: place your code here
    }
    
    // TODO: place your function definitions here
}