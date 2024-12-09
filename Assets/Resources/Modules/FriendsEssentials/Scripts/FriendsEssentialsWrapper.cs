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

        lobby.OnIncomingFriendRequest += OnIncomingFriendRequest;
        lobby.FriendRequestCanceled += OnFriendRequestCanceled;
        lobby.FriendRequestRejected += OnFriendRequestRejected;
        lobby.FriendRequestAccepted += OnFriendRequestAccepted;
    }
    
    private void OnDestroy()
    {
        AuthEssentialsWrapper.OnUserProfileReceived -= SetPlayerInfo;
        SinglePlatformAuthWrapper.OnUserProfileReceived -= SetPlayerInfo;
        AuthEssentialsWrapper_Starter.OnUserProfileReceived -= SetPlayerInfo;
        SinglePlatformAuthWrapper_Starter.OnUserProfileReceived -= SetPlayerInfo;

        lobby.OnIncomingFriendRequest -= OnIncomingFriendRequest;
        lobby.FriendRequestCanceled -= OnFriendRequestCanceled;
        lobby.FriendRequestRejected -= OnFriendRequestRejected;
        lobby.FriendRequestAccepted -= OnFriendRequestAccepted;
    }
    
    private void SetPlayerInfo(UserProfile userProfile)
    {
        PlayerUserId = userProfile.userId;
        PlayerFriendCode = userProfile.publicId;
    }

    #region Add Friends

    public void LoadIncomingFriendRequests(ResultCallback<Friends> resultCallback)
    {
        lobby.ListIncomingFriends(result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error loading incoming friend requests, " +
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
                BytewarsLogger.LogWarning("Error declining friend request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully rejected friend request with User Id: {userId}");
                CachedFriendUserIds.Remove(userId);
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
                BytewarsLogger.LogWarning("Error accepting friend request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully accepted friend request with User Id: {userId}");
                CachedFriendUserIds.Add(userId);
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
                BytewarsLogger.LogWarning("Error loading outgoing friend requests, " +
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
                BytewarsLogger.LogWarning("Error canceling friend request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully canceled outgoing friend request with User Id: {userId}");
                CachedFriendUserIds.Remove(userId);
            }

            resultCallback?.Invoke(result);
        });
    }

    public void GetBulkUserInfo(string[] userIds, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        user.BulkGetUserInfo(userIds, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error getting bulk user info, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved bulk user info");
                CachedFriendUserIds.Add(userIds);
            }

            resultCallback?.Invoke(result);
        });
    }
    
    private void OnIncomingFriendRequest(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error receiving incoming friend request notification, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Incoming friend request from {result.Value.friendId}");
        CachedFriendUserIds.Add(result.Value.friendId);

        OnIncomingRequest?.Invoke(result.Value.friendId);
    }
    
    private void OnFriendRequestCanceled(Result<Acquaintance> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error receiving friend request canceled notification, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.userId} has been canceled");
        CachedFriendUserIds.Remove(result.Value.userId);

        OnRequestCanceled?.Invoke(result.Value.userId);
    }
    
    private void OnFriendRequestRejected(Result<Acquaintance> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error receiving friend request rejected notification, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.userId} has been rejected");
        CachedFriendUserIds.Remove(result.Value.userId);

        OnRequestRejected?.Invoke(result.Value.userId);
    }
    
    private void OnFriendRequestAccepted(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error receiving friend request accepted notification, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Friend request from {result.Value.friendId} has been accepted");
        CachedFriendUserIds.Add(result.Value.friendId);

        OnRequestAccepted?.Invoke(result.Value.friendId);
    }

    #endregion Add Friends

    #region Search for Players

    public void GetUserByFriendCode(string friendCode, ResultCallback<PublicUserData> resultCallback)
    {
        userProfiles.GetUserProfilePublicInfoByPublicId(friendCode, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error getting user profile public info by public id, " +
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
                BytewarsLogger.LogWarning("Error getting user by user id, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully retrieved user by user id.");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void GetFriendshipStatus(string userId, ResultCallback<FriendshipStatus> resultCallback)
    {
        lobby.GetFriendshipStatus(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error getting friendship status, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved friendship status for {userId}");
            }

            resultCallback.Invoke(result);
        });
    }

    public void GetUserByExactDisplayName(string displayName, ResultCallback<PublicUserInfo> resultCallback)
    {
        const SearchType searchBy = SearchType.DISPLAYNAME;

        user.SearchUsers(displayName, searchBy, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error searching users by display name, " +
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

    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        user.GetUserAvatar(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error getting Avatar, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully retrieved Avatar for User Id: {userId}");
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
                BytewarsLogger.LogWarning("Error sending Friends Request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully sent Friends Request");
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
                BytewarsLogger.LogWarning("Error loading friends list, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
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
