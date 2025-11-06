// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper_Starter : MonoBehaviour
{
    // Optional Parameters
    public LoginV4OptionalParameters OptionalParameters = new();

    // AGS Game SDK references
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

    private void Awake()
    {
        user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        userProfiles = AccelByteSDK.GetClientRegistry().GetApi().GetUserProfiles();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        // TODO: Add the tutorial module code here.
    }

    void OnDestroy()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.
}
