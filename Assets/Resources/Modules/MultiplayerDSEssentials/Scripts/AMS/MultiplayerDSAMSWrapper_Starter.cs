// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class MultiplayerDSAMSWrapper_Starter: MonoBehaviour
{
    public event Action OnAMSConnectionOpened = delegate {};
    public event Action OnAMSDrainReceived = delegate {};
    
#if UNITY_SERVER

    // Put your code here
    

    void Start()
    {
        // Put your code here
        
    }

    // Put your code here
    

#endif
}