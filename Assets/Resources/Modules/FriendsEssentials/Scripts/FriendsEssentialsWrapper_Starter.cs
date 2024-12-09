// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Rendering;

public class FriendsEssentialsWrapper_Starter : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

    public string PlayerUserId { get; private set; } = string.Empty;
    public string PlayerFriendCode { get; private set; } = string.Empty;
    public ObservableList<string> CachedFriendUserIds { get; private set; } = new ObservableList<string>();

    public static event Action<string> OnIncomingRequest = delegate { };
    public static event Action<string> OnRequestCanceled = delegate { };
    public static event Action<string> OnRequestRejected = delegate { };
    public static event Action<string> OnRequestAccepted = delegate { };
    
    private void Awake()
    {
        user = ApiClient.GetUser();
        userProfiles = ApiClient.GetUserProfiles();
        lobby = ApiClient.GetLobby();

        // Assign to both starter and non to make sure we support mix matched modules starter mode
        AuthEssentialsWrapper.OnUserProfileReceived += SetPlayerInfo;
        SinglePlatformAuthWrapper.OnUserProfileReceived += SetPlayerInfo;
        AuthEssentialsWrapper_Starter.OnUserProfileReceived += SetPlayerInfo;
        SinglePlatformAuthWrapper_Starter.OnUserProfileReceived += SetPlayerInfo;

        // TODO: Bind listeners here.
    }
    
    private void OnDestroy()
    {
        AuthEssentialsWrapper.OnUserProfileReceived -= SetPlayerInfo;
        SinglePlatformAuthWrapper.OnUserProfileReceived -= SetPlayerInfo;
        AuthEssentialsWrapper_Starter.OnUserProfileReceived -= SetPlayerInfo;
        SinglePlatformAuthWrapper_Starter.OnUserProfileReceived -= SetPlayerInfo;

        // TODO: Unbind listeners here.
    }

    private void SetPlayerInfo(UserProfile userProfile)
    {
        PlayerUserId = userProfile.userId;
        PlayerFriendCode = userProfile.publicId;
    }

    #region Add Friends

    // TODO: Implement Add Friends Module functions here.

    #endregion Add Friends

    #region Search for Players

    // TODO: Implement Search for Players Module functions here.

    #endregion Search for Players

    #region Friend List

    // TODO: Implement Friend List Module functions here.

    #endregion
}
