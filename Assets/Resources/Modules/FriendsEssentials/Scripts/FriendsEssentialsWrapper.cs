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
    private PublicUserProfile cachedUserProfile;

    public ObservableList<string> CachedFriendUserIds { get; private set; } = new ObservableList<string>();

    private void Awake()
    {
        user = ApiClient.GetUser();
        userProfiles = ApiClient.GetUserProfiles();
        lobby = ApiClient.GetLobby();

        lobby.OnIncomingFriendRequest += OnIncomingFriendRequest;
        lobby.FriendRequestCanceled += OnFriendRequestCanceled;
        lobby.FriendRequestRejected += OnFriendRequestRejected;
        lobby.FriendRequestAccepted += OnFriendRequestAccepted;
    }
    
    private void OnDestroy()
    {
        lobby.OnIncomingFriendRequest -= OnIncomingFriendRequest;
        lobby.FriendRequestCanceled -= OnFriendRequestCanceled;
        lobby.FriendRequestRejected -= OnFriendRequestRejected;
        lobby.FriendRequestAccepted -= OnFriendRequestAccepted;
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

    public void GetBulkUserInfo(string[] userIds, ResultCallback<AccountUserPlatformInfosResponse> resultCallback)
    {
        user.GetUserOtherPlatformBasicPublicInfo("ACCELBYTE", userIds, result => 
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

        FriendsEssentialsModels.OnIncomingRequest?.Invoke(result.Value.friendId);
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

        FriendsEssentialsModels.OnRequestCanceled?.Invoke(result.Value.userId);
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

        FriendsEssentialsModels.OnRequestRejected?.Invoke(result.Value.userId);
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

        FriendsEssentialsModels.OnRequestAccepted?.Invoke(result.Value.friendId);
    }

    #endregion Add Friends

    #region Search for Players

    public void GetSelfFriendCode(ResultCallback<string> resultCallback)
    {
        string userId = GameData.CachedPlayerState.PlayerId;
        if (string.IsNullOrEmpty(userId))
        {
            string errorMessage = "Error to get self friend code. Failed to find the logged-in user Id.";
            BytewarsLogger.LogWarning(errorMessage);
            resultCallback.Invoke(Result<string>.CreateError(ErrorCode.InvalidArgument, errorMessage));
            return;
        }

        // Use cache if available.
        if (userId == cachedUserProfile?.userId)
        {
            resultCallback.Invoke(Result<string>.CreateOk(cachedUserProfile.publicId));
            return;
        }

        // Query the user profile to get its friend code (public ID).
        userProfiles.GetPublicUserProfile(userId, (Result<PublicUserProfile> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Error to get self friend code. Error: {result.Error.Message}");
                resultCallback.Invoke(Result<string>.CreateError(result.Error.Code, result.Error.Message));
                return;
            }

            BytewarsLogger.Log($"Successfully get self friend code for User Id: {userId}");

            cachedUserProfile = result.Value;
            resultCallback.Invoke(Result<string>.CreateOk(cachedUserProfile.publicId));
        });
    }

    public void GetUserByFriendCode(string friendCode, ResultCallback<AccountUserPlatformData> resultCallback)
    {
        userProfiles.GetUserProfilePublicInfoByPublicId(friendCode, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning(
                    $"Error getting user profile public info by public id. " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
                resultCallback?.Invoke(Result<AccountUserPlatformData>.CreateError(result.Error.Code, result.Error.Message));
                return;
            }

            GetBulkUserInfo(new string[] { result.Value.userId }, result =>
            {
                if (result.IsError) 
                {
                    BytewarsLogger.LogWarning(
                        $"Error getting user profile public info by public id. " +
                        $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
                    resultCallback?.Invoke(Result<AccountUserPlatformData>.CreateError(result.Error.Code, result.Error.Message));
                    return;
                }

                BytewarsLogger.Log("Successfully retrieved user profile public info by public id.");

                AccountUserPlatformData userData = result.Value.Data[0];
                CachedFriendUserIds.Add(userData.UserId);
                resultCallback?.Invoke(Result<AccountUserPlatformData>.CreateOk(userData));
            });
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
