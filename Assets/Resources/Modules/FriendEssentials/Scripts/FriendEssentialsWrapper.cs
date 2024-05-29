// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class FriendEssentialsWrapper : MonoBehaviour
{
    private User user;
    private UserProfiles userProfiles;
    private Lobby lobby;

    public string PlayerUserId { get; private set; }
    public string PlayerFriendCode { get; private set; }

    public static event Action OnRejected;
    public static event Action OnIncomingAdded;
    public static event Action OnAccepted;

    private void Awake()
    {
        user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        userProfiles = AccelByteSDK.GetClientRegistry().GetApi().GetUserProfiles();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        AuthEssentialsWrapper.OnUserProfileReceived += userProfile => PlayerFriendCode = userProfile.publicId;

        LoginHandler.onLoginCompleted += _ => ListenIncomingFriendRequest();
        LoginHandler.onLoginCompleted += _ => ListenRejectedRequest();
        LoginHandler.onLoginCompleted += _ => ListenAcceptedRequest();
        LoginHandler.onLoginCompleted += tokenData => PlayerUserId = tokenData.user_id;
    }

    #region Add Friends

    public void LoadIncomingFriendRequests(ResultCallback<Friends> resultCallback)
    {
        lobby.ListIncomingFriends(result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully loaded incoming friend requests");
            }
            else
            {
                BytewarsLogger.LogWarning($"Error ListIncomingFriends, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void DeclineFriend(string userId, ResultCallback resultCallback)
    {
        lobby.RejectFriend(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully rejected friend request with User Id: {userId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void AcceptFriend(string userId, ResultCallback resultCallback)
    {
        lobby.AcceptFriend(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully accepted friend request with User Id: {userId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void LoadOutgoingFriendRequests(ResultCallback<Friends> resultCallback = null)
    {
        lobby.ListOutgoingFriends(result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully loaded outgoing friend requests");
            }
            else
            {
                BytewarsLogger.LogWarning("Error ListOutgoingFriends," +
                                          $" Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void CancelFriendRequests(string userId, ResultCallback resultCallback)
    {
        lobby.CancelFriendRequest(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully canceled outgoing friend request with User Id: {userId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void GetBulkUserInfo(string[] usersId, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        user.BulkGetUserInfo(usersId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully retrieved bulk user info {!result.IsError}");

                resultCallback?.Invoke(result);
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
        });
    }
    
    private void ListenIncomingFriendRequest()
    {
        lobby.OnIncomingFriendRequest += result =>
        {
            if (!result.IsError)
            {
                OnIncomingAdded?.Invoke();
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
        };
    }
    
    private void ListenRejectedRequest()
    {
        lobby.FriendRequestRejected += result =>
        {
            if (!result.IsError)
            {
                OnRejected?.Invoke();
            }
            else
            {
                BytewarsLogger.LogWarning($"Error OnUnfriend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
        };
    }
    
    private void ListenAcceptedRequest()
    {
        lobby.FriendRequestAccepted += result =>
        {
            if (!result.IsError)
            {
                OnAccepted?.Invoke();
            }
            else
            {
                BytewarsLogger.LogWarning($"Error OnUnfriend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
        };
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

            resultCallback?.Invoke(Result<PublicUserInfo>.CreateOk(userByExactDisplayName));
        });
    }

    public void GetFriendshipStatus(string userId, ResultCallback<FriendshipStatus> resultCallback)
    {
        lobby.GetFriendshipStatus(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully retrieved friendship status for {userId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }

            resultCallback.Invoke(result);
        });
    }

    public void GetUserByFriendCode(string friendCode, ResultCallback<PublicUserData> resultCallback)
    {
        userProfiles.GetUserProfilePublicInfoByPublicId(friendCode, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully retrieved user profile public info by public id.");

                GetUserByUserId(result.Value.userId, resultCallback);
            }
            else
            {
                BytewarsLogger.LogWarning("Error GetUserProfilePublicInfoByPublicId, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

                resultCallback?.Invoke(Result<PublicUserData>.CreateError(result.Error.Code, result.Error.Message));
            }
        });
    }

    private void GetUserByUserId(string userId, ResultCallback<PublicUserData> resultCallback)
    {
        user.GetUserByUserId(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully retrieved user by user id.");
            }
            else
            {
                BytewarsLogger.LogWarning("Error GetUserByUserId, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    public void SendFriendRequest(string userId, ResultCallback resultCallback)
    {
        lobby.RequestFriend(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully sent Friends Request");
                resultCallback?.Invoke(result);
            }
            else
            {
                BytewarsLogger.LogWarning($"Failed to send a friends request: error code: {result.Error.Code} message: {result.Error.Message}");
            }
        });
    }

    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        user.GetUserAvatar(userId, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Successfully retrieved Avatar for User Id: {userId}");
            }
            else
            {
                BytewarsLogger.LogWarning($"Unable to retrieve Avatar for User {userId} : {result.Error}");
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
            if (!result.IsError)
            {
                BytewarsLogger.Log("Successfully loaded friends list");
            }
            else
            {
                BytewarsLogger.LogWarning(
                    $"Error LoadFriendsList, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }

            resultCallback?.Invoke(result);
        });
    }

    #endregion Friend List
}
