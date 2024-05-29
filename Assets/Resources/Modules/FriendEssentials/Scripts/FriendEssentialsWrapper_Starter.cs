// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class FriendEssentialsWrapper_Starter : MonoBehaviour
{
    #region Predefined-8a

    private User user;
    private Lobby lobby;

    public string PlayerUserId { get; private set; }
    public string PlayerFriendCode { get; private set; }

    #endregion

    #region Predefined-8b

    public static event Action OnRejected;
    public static event Action OnIncomingAdded;
    public static event Action OnAccepted;

    #endregion

    private void Awake()
    {
        // Predefined code
        user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        AuthEssentialsWrapper.OnUserProfileReceived += userProfile => PlayerFriendCode = userProfile.publicId;

        LoginHandler.onLoginCompleted += tokenData =>
        {
            PlayerUserId = tokenData.user_id;
        };
    }
}
