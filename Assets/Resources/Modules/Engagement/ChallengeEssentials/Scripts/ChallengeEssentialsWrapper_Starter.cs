// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Api.Interface;
using AccelByte.Core;
using AccelByte.Models;
using UnityEditor;
using UnityEngine;
using static ChallengeEssentialsModels;

public class ChallengeEssentialsWrapper_Starter : MonoBehaviour
{
    // AGS Game SDK references
    private IClientChallenge challenge;
    private Items items;

    // TODO: Declare the tutorial module variables here.

    private void Awake()
    {
        challenge = AccelByteSDK.GetClientRegistry().GetApi().GetChallenge();
        items = AccelByteSDK.GetClientRegistry().GetApi().GetItems();
    }

    // TODO: Declare the tutorial module functions here.
}
