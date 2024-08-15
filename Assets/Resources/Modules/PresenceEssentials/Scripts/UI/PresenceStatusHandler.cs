// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Models;
using TMPro;
using UnityEngine;

public class PresenceStatusHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text presenceStatusText;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Default;

    private enum DisplayMode
    {
        Default,
        ShowAsSeparateLines,
        ShowActivityOverAvailability
    }
    
    private PresenceEssentialsWrapper presenceEssentialsWrapper;
    
    private string UserId { get; set; } = string.Empty;
    
    private void OnEnable()
    {
        if (presenceEssentialsWrapper != null)
        {
            SetupPresence();
        }
    }
    
    private void Awake()
    {
        presenceStatusText.text = string.Empty;
        
        PresenceEssentialsWrapper.OnFriendsStatusChanged += OnFriendsStatusChanged;
        PresenceEssentialsWrapper.OnBulkUserStatusReceived += OnBulkUserStatusReceived;
    }
    
    private void Start()
    {
        presenceEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<PresenceEssentialsWrapper>();
        if (presenceEssentialsWrapper == null)
        {
            BytewarsLogger.LogWarning("PresenceEssentialsWrapper is null, please check if the module is enabled in the Asset Config.");
            return;
        }

        SetupPresence();
    }
    
    private void OnDestroy()
    {
        PresenceEssentialsWrapper.OnFriendsStatusChanged -= OnFriendsStatusChanged;
        PresenceEssentialsWrapper.OnBulkUserStatusReceived -= OnBulkUserStatusReceived;
    }

    #region User Presence Module

    #region Main Functions

    private void SetupPresence()
    {
        UserId = GetUserIdFromParent();
        
        if (string.IsNullOrEmpty(UserId))
        {
            BytewarsLogger.LogWarning("UserId is empty");
            return;
        }
        
        UserStatusNotif userStatus = presenceEssentialsWrapper.GetUserStatus(UserId);
        if (userStatus == null)
        {
            return;
        }
        
        SetPresenceStatusText(userStatus.availability, userStatus.lastSeenAt, userStatus.activity);
    }
    
    private string GetUserIdFromParent()
    {
        Transform parent = transform.parent;
        
        while (parent != null)
        {
            if (parent.TryGetComponent(out IFriendsEntryHandler friendsEntry))
            {
                return friendsEntry.UserId;
            }
            
            if (parent.TryGetComponent(out FriendDetailsMenuHandler friendDetails))
            {
                return friendDetails.UserId;
            }
            
            if (parent.TryGetComponent(out FriendDetailsMenuHandler_Starter friendDetailsStarter))
            {
                return friendDetailsStarter.UserId;
            }
            
            if (parent.parent == null)
            {
                BytewarsLogger.LogWarning("Parent transform not found");
                
                return string.Empty;
            }
            
            parent = parent.parent;
        }
        
        return string.Empty;
    }

    #endregion Main Functions

    #region Callback Functions

    private void OnFriendsStatusChanged(FriendsStatusNotif friendsStatus)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }
        
        if (friendsStatus.userID != UserId)
        {
            return;
        }
        
        SetPresenceStatusText(friendsStatus.availability, friendsStatus.lastSeenAt, friendsStatus.activity);
    }
    
    private void OnBulkUserStatusReceived(BulkUserStatusNotif bulkUserStatusNotif)
    {
        if (bulkUserStatusNotif.data.Length <= 0)
        {
            BytewarsLogger.LogWarning("Bulk user status data is empty");
            return;
        }
        
        foreach (UserStatusNotif userStatus in bulkUserStatusNotif.data)
        {
            if (userStatus.userID != UserId)
            {
                continue;
            }
            
            SetPresenceStatusText(userStatus.availability, userStatus.lastSeenAt, userStatus.activity);
        }
    }

    #endregion Callback Functions

    #region View Management

    private void SetPresenceStatusText(UserStatus userStatus, DateTime lastOnline, string activity = null)
    {
        string statusText = string.Empty;

        bool activityUndefined = activity is PresenceHelper.DefaultActivityKeyword;
        if (activityUndefined)
        {
            activity = string.Empty;
        }

        switch (userStatus)
        {
            case UserStatus.Online when displayMode == DisplayMode.ShowAsSeparateLines:
                statusText = $"{PresenceHelper.AvailabilityStatus[UserStatus.Online]}\n{activity}";
                break;
            case UserStatus.Online when displayMode == DisplayMode.ShowActivityOverAvailability && !string.IsNullOrEmpty(activity):
                statusText = activity;
                break;
            case UserStatus.Online:
                statusText = PresenceHelper.AvailabilityStatus[UserStatus.Online];
                break;
            case UserStatus.Offline when displayMode == DisplayMode.ShowAsSeparateLines:
                statusText = $"{PresenceHelper.AvailabilityStatus[UserStatus.Offline]}\n{PresenceHelper.GetLastOnline(lastOnline)}";
                break;
            case UserStatus.Offline:
                statusText = PresenceHelper.GetLastOnline(lastOnline);
                break;
            default:
                break;
        }
        
        presenceStatusText.text = statusText;
    }

    #endregion View Management

    #endregion User Presence Module
}
