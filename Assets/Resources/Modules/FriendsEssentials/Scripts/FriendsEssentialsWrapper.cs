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

public class FriendsEssentialsWrapper : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private User user;
    private UserProfiles userProfiles ;
    private Lobby lobby;

    public string PlayerUserId { get; private set; } = string.Empty;
    public string PlayerFriendCode { get; private set; } = string.Empty;
    public ObservableList<string> CachedFriendUserIds { get; private set; } = new ObservableList<string>();

    public static event Action<string> OnIncomingRequest = delegate { };
    public static event Action<string> OnRequestCanceled = delegate { };
    public static event Action<string> OnRequestRejected = delegate { };
    public static event Action<string> OnRequestAccepted = delegate { };
    public static event Action<string> OnUnfriended = delegate { };
    
    private void Awake()
    {
        user = ApiClient.GetUser();
        userProfiles = ApiClient.GetUserProfiles();
        lobby = ApiClient.GetLobby();

        LoginHandler.onLoginCompleted += CheckLobbyConnection;
        AuthEssentialsWrapper.OnUserProfileReceived += SetPlayerInfo;
        SinglePlatformAuthWrapper.OnUserProfileReceived += SetPlayerInfo;

        lobby.OnIncomingFriendRequest += OnIncomingFriendRequest;
        lobby.FriendRequestCanceled += OnFriendRequestCanceled;
        lobby.FriendRequestRejected += OnFriendRequestRejected;
        lobby.FriendRequestAccepted += OnFriendRequestAccepted;
        lobby.OnUnfriend += OnUserUnfriended;
    }
    
    private void OnDestroy()
    {
        LoginHandler.onLoginCompleted -= CheckLobbyConnection;
        AuthEssentialsWrapper.OnUserProfileReceived -= SetPlayerInfo;
        SinglePlatformAuthWrapper.OnUserProfileReceived -= SetPlayerInfo;

        lobby.OnIncomingFriendRequest -= OnIncomingFriendRequest;
        lobby.FriendRequestCanceled -= OnFriendRequestCanceled;
        lobby.FriendRequestRejected -= OnFriendRequestRejected;
        lobby.FriendRequestAccepted -= OnFriendRequestAccepted;
        lobby.OnUnfriend -= OnUserUnfriended;
    }
    
    private void SetPlayerInfo(UserProfile userProfile)
    {
        PlayerUserId = userProfile.userId;
        PlayerFriendCode = userProfile.publicId;
    }

    private void CheckLobbyConnection(TokenData tokenData)
    {
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }
    }

    #region Add Friends

    public void LoadIncomingFriendRequests(ResultCallback<Friends> resultCallback)
    {
        lobby.ListIncomingFriends(result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error ListIncomingFriends, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully loaded incoming friend requests");

                CachedFriendUserIds.Add(result.Value.friendsId);
            }

            resultCallback?.Invoke(result);
        });
    }

    public void DeclineFriend(string userId, ResultCallback resultCallback)
    {
        lobby.RejectFriend(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully rejected friend request with User Id: {userId}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void AcceptFriend(string userId, ResultCallback resultCallback)
    {
        lobby.AcceptFriend(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully accepted friend request with User Id: {userId}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void LoadOutgoingFriendRequests(ResultCallback<Friends> resultCallback = null)
    {
        lobby.ListOutgoingFriends(result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error ListOutgoingFriends," +
                                          $" Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully loaded outgoing friend requests");

                CachedFriendUserIds.Add(result.Value.friendsId);
            }

            resultCallback?.Invoke(result);
        });
    }

    public void CancelFriendRequests(string userId, ResultCallback resultCallback)
    {
        lobby.CancelFriendRequest(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully canceled outgoing friend request with User Id: {userId}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void GetBulkUserInfo(string[] usersId, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        user.BulkGetUserInfo(usersId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved bulk user info");
            }

            resultCallback?.Invoke(result);
        });
    }
    
    private void OnIncomingFriendRequest(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            
            return;
        }

        BytewarsLogger.Log($"Incoming friend request from {result.Value.friendId}");

        CachedFriendUserIds.Add(result.Value.friendId);

        OnIncomingRequest?.Invoke(result.Value.friendId);
    }
    
    private static void OnFriendRequestCanceled(Result<Acquaintance> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error OnFriendRequestCanceled, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.userId} has been canceled");

        OnRequestCanceled?.Invoke(result.Value.userId);
    }
    
    private static void OnFriendRequestRejected(Result<Acquaintance> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error OnFriendRequestRejected, " + 
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            
            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.userId} has been rejected");

        OnRequestRejected?.Invoke(result.Value.userId);
    }
    
    private static void OnFriendRequestAccepted(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error OnFriendRequestAccepted, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            
            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.friendId} has been accepted");

        OnRequestAccepted?.Invoke(result.Value.friendId);
    }
    
    private void OnUserUnfriended(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error OnUserUnfriended, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

            return;
        }

        BytewarsLogger.Log($"Unfriend from {result.Value.friendId}");

        CachedFriendUserIds.Remove(result.Value.friendId);

        OnUnfriended?.Invoke(result.Value.friendId);
    }

    #endregion Add Friends

    #region Search for Players

    public void GetUserByExactDisplayName(string displayName, ResultCallback<PublicUserInfo> resultCallback)
    {
        const SearchType searchBy = SearchType.DISPLAYNAME;

        user.SearchUsers(displayName, searchBy, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Error SearchUsers, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

                resultCallback?.Invoke(Result<PublicUserInfo>.CreateError(result.Error.Code, result.Error.Message));
                return;
            }

            PublicUserInfo userByExactDisplayName = result.Value.data.FirstOrDefault(publicUserInfo => 
                publicUserInfo.displayName.Equals(displayName, StringComparison.CurrentCultureIgnoreCase));

            if (userByExactDisplayName == null)
            {
                BytewarsLogger.LogWarning($"User with display name {displayName} not found.");

                resultCallback?.Invoke(Result<PublicUserInfo>.CreateError(ErrorCode.UserNotFound, "User not found."));
                return;
            }

            BytewarsLogger.Log($"Successfully found users for display name: {displayName}");

            CachedFriendUserIds.Add(userByExactDisplayName.userId);

            resultCallback?.Invoke(Result<PublicUserInfo>.CreateOk(userByExactDisplayName));
        });
    }

    public void GetFriendshipStatus(string userId, ResultCallback<FriendshipStatus> resultCallback)
    {
        lobby.GetFriendshipStatus(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved friendship status for {userId}");
            }

            resultCallback.Invoke(result);
        });
    }

    public void GetUserByFriendCode(string friendCode, ResultCallback<PublicUserData> resultCallback)
    {
        userProfiles.GetUserProfilePublicInfoByPublicId(friendCode, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error GetUserProfilePublicInfoByPublicId, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

                resultCallback?.Invoke(Result<PublicUserData>.CreateError(result.Error.Code, result.Error.Message));

                return;
            }

            BytewarsLogger.Log("Successfully retrieved user profile public info by public id.");

            CachedFriendUserIds.Add(result.Value.userId);

            GetUserByUserId(result.Value.userId, resultCallback);
        });
    }

    private void GetUserByUserId(string userId, ResultCallback<PublicUserData> resultCallback)
    {
        user.GetUserByUserId(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error GetUserByUserId, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully retrieved user by user id.");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void SendFriendRequest(string userId, ResultCallback resultCallback)
    {
        lobby.RequestFriend(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to send a friends request: error code: {result.Error.Code} message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully sent Friends Request");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        user.GetUserAvatar(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Unable to retrieve Avatar for User {userId} : {result.Error}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved Avatar for User Id: {userId}");
            }

            resultCallback?.Invoke(result);
        });
    }

    #endregion Search for Players

    #region Friend List

    public void GetFriendList(ResultCallback<Friends> resultCallback = null)
    {
        lobby.LoadFriendsList(result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Error LoadFriendsList, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully loaded friends list");

                CachedFriendUserIds.Add(result.Value.friendsId);
            }

            resultCallback?.Invoke(result);
        });
    }

    #endregion Friend List
}
