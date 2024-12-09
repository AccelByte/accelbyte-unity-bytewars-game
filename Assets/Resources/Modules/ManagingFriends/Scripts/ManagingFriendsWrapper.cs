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

public class ManagingFriendsWrapper : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private static Lobby lobby;

    private FriendsEssentialsWrapper friendsEssentialsWrapper;

    public static event Action<string> OnPlayerBlocked = delegate { };
    public static event Action<string> OnPlayerUnfriended = delegate { };

    private void Awake()
    {
        lobby = ApiClient.GetLobby();

        lobby.PlayerBlockedNotif += OnPlayerBlockedNotif;
        lobby.OnUnfriend += OnPlayerUnfriendNotif;
    }
    
    private void Start()
    {
        friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
    }
    
    private void OnDestroy()
    {
        lobby.PlayerBlockedNotif -= OnPlayerBlockedNotif;
        lobby.OnUnfriend -= OnPlayerUnfriendNotif;
    }

    #region Manage Friends

    public void GetBlockedPlayers(ResultCallback<BlockedList> resultCallback)
    {
        lobby.GetListOfBlockedUser(result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error to load blocked users, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to load blocked users, total blocked users {result.Value.data.Length}");

                IEnumerable<string> blockedUserIds = result.Value.data.Select(user => user.blockedUserId);
                friendsEssentialsWrapper.CachedFriendUserIds.Add(blockedUserIds.ToArray());
            }

            resultCallback?.Invoke(result);
        });
    }

    public void Unfriend(string userId, ResultCallback resultCallback)
    {
        lobby.Unfriend(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error sending unfriend request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully unfriended player with user Id: {userId}");
                
                friendsEssentialsWrapper.CachedFriendUserIds.Remove(userId);
            }
            
            resultCallback?.Invoke(result);
        });
    }
    
    public void BlockPlayer(string userId, ResultCallback<BlockPlayerResponse> resultCallback)
    {
        lobby.BlockPlayer(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error sending block player request, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully blocked player with user Id: {userId}");
            }
            
            resultCallback?.Invoke(result);
        });
    }
    
    public void UnblockPlayer(string userId, ResultCallback<UnblockPlayerResponse> resultCallback)
    {
        lobby.UnblockPlayer(userId, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error unblock a friend, " +
                    $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Successfully unblocked player with user Id: {userId}");

                friendsEssentialsWrapper.CachedFriendUserIds.Remove(userId);
            }

            resultCallback?.Invoke(result);
        });
    }
    
    private static void OnPlayerBlockedNotif(Result<PlayerBlockedNotif> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error retrieving player blocked notif, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");

            return;
        }

        BytewarsLogger.Log($"Player with user Id: {result.Value.userId} has been blocked");

        OnPlayerBlocked?.Invoke(result.Value.userId);
    }

    private void OnPlayerUnfriendNotif(Result<Friend> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error receiving unfriend notification, " +
                $"Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Unfriend from {result.Value.friendId}");

        OnPlayerUnfriended?.Invoke(result.Value.friendId);
    }

    #endregion Manage Friends
}
