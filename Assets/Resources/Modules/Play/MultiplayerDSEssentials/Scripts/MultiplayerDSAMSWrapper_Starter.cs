// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class MultiplayerDSAMSWrapper_Starter : MonoBehaviour
{
    public event Action OnAMSConnectionOpened = delegate { };
    public event Action OnAMSConnectionClosed = delegate { };
    public event Action OnAMSDrainSignalReceived = delegate { };

    private DedicatedServer ds;
    private ServerAMS ams;
    private ServerDSHub dsHub;

    private readonly int ServerShutdownDelay = 10;

#if UNITY_SERVER
    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }
#endif

}