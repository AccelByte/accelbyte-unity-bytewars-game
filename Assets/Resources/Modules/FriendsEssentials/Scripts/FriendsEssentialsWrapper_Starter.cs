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
    #region Predefined - Friends Essentials

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
    public static event Action<string> OnUnfriended = delegate { };

    #endregion Predefined - Friends Essentials

    private void Awake()
    {
        user = ApiClient.GetUser();
        userProfiles = ApiClient.GetUserProfiles();
        lobby = ApiClient.GetLobby();

        LoginHandler.onLoginCompleted += CheckLobbyConnection;
    }
    
    private void OnDestroy()
    {
        LoginHandler.onLoginCompleted -= CheckLobbyConnection;
    }
    
    private void SetPlayerInfo(UserProfile userProfile)
    {
        BytewarsLogger.LogWarning("The SetPlayerInfo method is not implemented yet");
    }
    
    private void CheckLobbyConnection(TokenData tokenData)
    {
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }
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
