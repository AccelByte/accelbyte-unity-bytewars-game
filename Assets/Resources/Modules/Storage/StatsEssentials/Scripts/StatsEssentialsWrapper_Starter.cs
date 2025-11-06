// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;

public class StatsEssentialsWrapper_Starter : MonoBehaviour
{
    // AGS Game SDK references
    private Statistic statistic;
    private ServerStatistic serverStatistic;

    void Start()
    {
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
#if UNITY_SERVER
        serverStatistic = AccelByteSDK.GetServerRegistry().GetApi().GetStatistic();
#endif

        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.
}
