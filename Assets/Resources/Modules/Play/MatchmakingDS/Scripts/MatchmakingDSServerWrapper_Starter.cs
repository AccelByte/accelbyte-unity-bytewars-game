// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;

public class MatchmakingDSServerWrapper_Starter: MatchmakingEssentialsWrapper
{
#if UNITY_SERVER
    protected override void Awake()
    {
        base.Awake();

        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.
#endif
}
