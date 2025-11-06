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
    private PublicUserProfile cachedUserProfile;

    public ObservableList<string> CachedFriendUserIds { get; private set; } = new ObservableList<string>();
    
    private void Awake()
    {
        user = ApiClient.GetUser();
        userProfiles = ApiClient.GetUserProfiles();
        lobby = ApiClient.GetLobby();

        // TODO: Bind listeners here.
    }
    
    private void OnDestroy()
    {
        // TODO: Unbind listeners here.
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
