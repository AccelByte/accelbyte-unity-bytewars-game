// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class LeaderboardEssentialsWrapper_Starter : MonoBehaviour
{
    // AGS Game SDK references
    private Leaderboard leaderboard;

    private void Start()
    {
        leaderboard = AccelByteSDK.GetClientRegistry().GetApi().GetLeaderboard();
    }

    // TODO: Declare the tutorial module functions here.
}
