// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PresenceEssentialsWrapper : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private Lobby lobby;
    
    private string SceneStatus { get; set; } = string.Empty;
    private string GameModeStatus { get; set; } = string.Empty;
    private string CachedActivity { get; set; } = string.Empty;
    private bool IsInParty { get; set; } = false;

    private readonly List<UserStatusNotif> cachedUserPresence = new();

    private FriendsEssentialsWrapper friendsEssentialsWrapper;
    public static event Action<FriendsStatusNotif> OnFriendsStatusChanged = delegate { };
    public static event Action<BulkUserStatusNotif> OnBulkUserStatusReceived = delegate { };

    private void Awake()
    {
        lobby = ApiClient.GetLobby();
        lobby.FriendsStatusChanged += OnStatusChanged;
        lobby.Connected += UpdateSelfPresenceStatus;

        MatchmakingEssentialsWrapper.OnMatchmakingStarted += OnMatchmakingStarted;
        MatchmakingEssentialsWrapper.OnMatchmakingCanceled += OnMatchmakingCanceled;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired += OnMatchmakingExpired;

        PartyEssentialsModels.PartyHelper.BindOnPartyUpdate(OnPartyUpdate);

        SceneManager.sceneLoaded += OnSceneLoaded;
        MenuManager.OnMenuChanged += OnMenuChanged;

        friendsEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<FriendsEssentialsWrapper>();
        friendsEssentialsWrapper.CachedFriendUserIds.ItemAdded += OnFriendUserIdAdded;
    }

    private void OnDestroy()
    {
        lobby.FriendsStatusChanged -= OnStatusChanged;
        lobby.Connected -= UpdateSelfPresenceStatus;

        MatchmakingEssentialsWrapper.OnMatchmakingStarted -= OnMatchmakingStarted;
        MatchmakingEssentialsWrapper.OnMatchmakingCanceled -= OnMatchmakingCanceled;
        MatchmakingEssentialsWrapper.OnMatchmakingExpired -= OnMatchmakingExpired;

        PartyEssentialsModels.PartyHelper.UnBindOnPartyUpdate(OnPartyUpdate);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        MenuManager.OnMenuChanged -= OnMenuChanged;

        friendsEssentialsWrapper.CachedFriendUserIds.ItemAdded -= OnFriendUserIdAdded;

        SetPresenceStatus(string.Empty, UserStatus.Offline);
    }

    #region User Presence Module

    #region Main Functions

    private void UpdateSelfPresenceStatus()
    {
        if (friendsEssentialsWrapper == null)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(SceneStatus))
        {
            AssignStatusBySceneAndMenu();
        }

        StringBuilder activityBuilder = new StringBuilder(SceneStatus);

        bool hasGameModeStatus = !string.IsNullOrEmpty(GameModeStatus);
        if (hasGameModeStatus)
        {
            activityBuilder.Append(PresenceEssentialsModels.HyphenSeparator).Append(GameModeStatus);
        }
        
        if (IsInParty)
        {
            activityBuilder
                .Append(hasGameModeStatus ? PresenceEssentialsModels.CommaSeparator : PresenceEssentialsModels.HyphenSeparator)
                .Append(PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.InAParty]);
        }

        string activity = activityBuilder.ToString();
        if (CachedActivity.Equals(activity))
        {
            return;
        }

        CachedActivity = activity;
        SetPresenceStatus(activity);
    }
    
    private void SetPresenceStatus(string activity, 
        UserStatus availability = UserStatus.Online, ResultCallback resultCallback = null)
    {
        lobby.SetUserStatus(availability, activity, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error setting user presence status, " 
                    + $"Error Code: {result.Error.Code}, Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully set user presence status");
            }
            
            resultCallback?.Invoke(result);
        });
    }
    
    private void BulkGetUserPresence(string[] userIds, ResultCallback<BulkUserStatusNotif> resultCallback)
    {
        lobby.BulkGetUserPresence(userIds, result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Error getting bulk user presence, " 
                    + $"Error Code: {result.Error.Code}, Error Message: {result.Error.Message}");

                resultCallback.Invoke(result);
                return;
            }

            BytewarsLogger.Log("Successfully retrieved bulk user presence");

            var updatedUserStatuses = result.Value.data.Select(userStatus => new UserStatusNotif
            {
                userID = userStatus.userID,
                availability = userStatus.availability,
                activity = Uri.UnescapeDataString(userStatus.activity),
                lastSeenAt = userStatus.lastSeenAt
            }).ToList();

            cachedUserPresence.RemoveAll(user => updatedUserStatuses.Any(updatedUser => updatedUser.userID == user.userID));
            cachedUserPresence.AddRange(updatedUserStatuses);

            resultCallback.Invoke(result);
        });
    }
    
    public UserStatusNotif GetUserStatus(string userId)
    {
        if (cachedUserPresence.Count <= 0)
        {
            return null;
        }
        
        if (cachedUserPresence.All(user => user.userID != userId))
        {
            return null;
        }
        
        return cachedUserPresence.First(user => user.userID == userId);
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnStatusChanged(Result<FriendsStatusNotif> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning("Error listening to friends status changed, "
                + $"Error Code: {result.Error.Code}, Error Message: {result.Error.Message}");
            return;
        }

        BytewarsLogger.Log($"Successfully retrieved friends status changed for userId: {result.Value.userID}, " 
            + $"activity: {result.Value.activity}, availability: {result.Value.availability}");

        UserStatusNotif userStatus = new()
        {
            userID = result.Value.userID,
            availability = result.Value.availability,
            activity = result.Value.activity,
            lastSeenAt = result.Value.lastSeenAt
        };

        bool newData = cachedUserPresence.All(user => user.userID != userStatus.userID);
        if (newData)
        {
            cachedUserPresence.Add(userStatus);
        }
        else
        {
            int overwriteIndex = cachedUserPresence.FindIndex(user => user.userID == userStatus.userID);
            cachedUserPresence[overwriteIndex] = userStatus;
        }

        OnFriendsStatusChanged?.Invoke(result.Value);
    }
    
    private void OnPartyUpdate()
    {
        IsInParty = PartyEssentialsModels.PartyHelper.CurrentPartySession != null;
        UpdateSelfPresenceStatus();
    }

    private void OnFriendUserIdAdded(ObservableList<string> sender, ListChangedEventArgs<string> e)
    {
        BulkGetUserPresence(sender.ToArray(), result =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning("Error getting bulk user presence, "
                    + $"Error Code: {result.Error.Code}, Error Message: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log("Successfully retrieved bulk user presence");
            }
            
            OnBulkUserStatusReceived?.Invoke(result.Value);
        });
    }

    private void OnMatchmakingStarted(Result<MatchmakingV2CreateTicketResponse> result)
    {
        // Update to show matchmaking activity status.
        bool inMainMenuScene = SceneManager.GetActiveScene().buildIndex is GameConstant.MenuSceneBuildIndex;
        if (inMainMenuScene && !result.IsError)
        {
            SceneStatus = "In Main Menu";
            GameModeStatus = "Matchmaking";
            UpdateSelfPresenceStatus();
        }
    }

    private void OnMatchmakingCanceled(Result result)
    {
        // Reset to remove the matchmaking activity status.
        if (!result.IsError)
        {
            AssignStatusBySceneAndMenu();
            UpdateSelfPresenceStatus();
        }
    }

    private void OnMatchmakingExpired(Result<MatchmakingV2TicketExpiredNotification> result)
    {
        // Reset to remove the matchmaking activity status.
        AssignStatusBySceneAndMenu();
        UpdateSelfPresenceStatus();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignStatusBySceneAndMenu();
        UpdateSelfPresenceStatus();
    }
    
    private void OnMenuChanged(MenuCanvas menuCanvas)
    {
        AssignStatusBySceneAndMenu(menuCanvas);

        bool shouldUpdatePresence = menuCanvas is MatchLobbyMenu or MainMenu;
        if (shouldUpdatePresence)
        {
            UpdateSelfPresenceStatus();
        }
    }
    
    private void AssignStatusBySceneAndMenu(MenuCanvas menuCanvas = null)
    {
        bool inMainMenuScene = SceneManager.GetActiveScene().buildIndex is GameConstant.MenuSceneBuildIndex;
        bool inGameScene = SceneManager.GetActiveScene().buildIndex is GameConstant.GameSceneBuildIndex;
        
        if (inMainMenuScene)
        {
            bool inMatchLobbyMenu = menuCanvas != null && menuCanvas is MatchLobbyMenu;
            
            SceneStatus = PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.InMainMenu];

            if (inMatchLobbyMenu)
            {
                GameModeStatus = PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.MatchLobby];
                return;
            }

            GameModeStatus = string.Empty;
            return;
        }
        
        if (inGameScene)
        {
            SceneStatus = PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.InAMatch];

            GameModeStatus = "Game Mode: " + GameManager.Instance.InGameMode switch
            {
                InGameMode.MatchmakingElimination => PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.Elimination],
                InGameMode.CreateMatchElimination => PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.Elimination],
                InGameMode.MatchmakingTeamDeathmatch => PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.TeamDeathmatch],
                InGameMode.CreateMatchTeamDeathmatch => PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.TeamDeathmatch],
                _ => PresenceEssentialsModels.ActivityStatus[PresenceEssentialsModels.PresenceActivity.Singleplayer]
            };
        }
    }

    #endregion Callback Functions

    #endregion User Presence Module

}
