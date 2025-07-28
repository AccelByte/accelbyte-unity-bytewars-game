// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Text;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using Unity.Profiling;
using UnityEngine;

public class MultiplayerDSArmadaWrapper_Starter: MonoBehaviour
{
    public event Action OnInstantiateComplete;
    public event ResultCallback OnLoginCompleteEvent;
    public event ResultCallback OnRegisterCompleteEvent;
    public event ResultCallback OnUnregisterCompleteEvent;
    
    private DedicatedServer ds;
    private DedicatedServerManager dsm;
    private ServerDSHub dsHub;
    
#if UNITY_SERVER
    // Put your code here
    

    private void Start()
    {
        // Put your code here
        
    }

    // Put your code here
    

#endif
}