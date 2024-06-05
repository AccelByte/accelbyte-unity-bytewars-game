// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class ManagingFriendsWrapper_Starter : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private static Lobby lobby;

    // TODO: Declare the dependency module wrapper here.

    public static event Action<string> OnPlayerBlocked = delegate { };

    private void Awake()
    {
        lobby = ApiClient.GetLobby();

        // TODO: Subscribe events here.
    }

    private void Start()
    {
        // TODO: Define the dependency module wrapper here.
    }

    private void OnDestroy()
    {
        // TODO: Unsubscribe events here.
    }

    #region Manage Friends

    // TODO: Implement Manage Friends Module functions here.

    #endregion Manage Friends
}
