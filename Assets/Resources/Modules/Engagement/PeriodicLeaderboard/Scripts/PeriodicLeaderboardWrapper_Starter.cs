// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System;
using System.Linq;

public class PeriodicLeaderboardWrapper_Starter : MonoBehaviour
{
    // AGS Game SDK references
    private Leaderboard leaderboard;
    private Statistic statistic;

    private void Start()
    {
        leaderboard = AccelByteSDK.GetClientRegistry().GetApi().GetLeaderboard();
        statistic = AccelByteSDK.GetClientRegistry().GetApi().GetStatistic();
    }

    // TODO: Declare the tutorial module functions here.
}
