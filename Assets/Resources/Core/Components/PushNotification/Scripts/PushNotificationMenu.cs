// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void PushNotificationActionCallback(PushNotificationActionResult result);

public enum PushNotificationActionResult
{
    Button1,
    Button2,
    Button3
}

public struct PushNotificationModel
{
    public string IconUrl;
    public bool UseDefaultIconOnEmpty;
    public string Message;
    public string[] ActionButtonTexts;
    public PushNotificationActionCallback ActionButtonCallback;
}

public class PushNotificationMenu : MonoBehaviour
{
    [SerializeField] private Transform notificationList;
    [SerializeField] private PushNotificationEntry notificationEntryPrefab;
    [SerializeField] private Button dismissButton;

    [SerializeField] private int notificationLifeTime = 10;
    [SerializeField] private int maxNotificationStack = 5;

    private List<PushNotificationModel> pendingNotifications = new List<PushNotificationModel>();
    private Dictionary<PushNotificationEntry, Coroutine> notificationTimers = new Dictionary<PushNotificationEntry, Coroutine>();

    #region Push Notification Handlers
    public void PushNotification(PushNotificationModel notification) 
    {
        // Mark as pending notification if the max stack is reached.
        if (notificationList.childCount >= maxNotificationStack) 
        {
            pendingNotifications.Add(notification);
            return;
        }

        // Insert new notification.
        PushNotificationEntry newEntry = Instantiate(notificationEntryPrefab, notificationList);
        newEntry.transform.SetAsFirstSibling();
        newEntry.Init(notification);

        // Start notification lifetime.
        notificationTimers.Add(newEntry, StartCoroutine(OnNotificationLifeTimeEnds(newEntry)));
    }

    public void RemoveNotification(PushNotificationEntry notificationEntry) 
    {
        // Delete from pending notifications.
        if (pendingNotifications.Contains(notificationEntry.CachedNotification))
        {
            pendingNotifications.Remove(notificationEntry.CachedNotification);
        }

        // Delete notification timer.
        if (notificationTimers.TryGetValue(notificationEntry, out Coroutine timer))
        {
            StopCoroutine(timer);
            notificationTimers.Remove(notificationEntry);
        }

        // Delete from notification list.
        DestroyImmediate(notificationEntry.gameObject);

        // Dismiss the notification if empty.
        if (pendingNotifications.Count <= 0 && notificationList.childCount <= 0)
        {
            DismissAllNotifications();
        }
    }

    public void DismissAllNotifications() 
    {
        // Clear pending notifications.
        pendingNotifications.Clear();

        // Clear dangling notification timers.
        foreach(Coroutine notificationTimer in notificationTimers.Values)
        {
            if (notificationTimer != null) 
            {
                StopCoroutine(notificationTimer);
            }
        }
        notificationTimers.Clear();

        // Clear notifications.
        notificationList.DestroyAllChildren();

        gameObject.SetActive(false);
    }
    
    private void TryPushPendingNotifications() 
    {
        int maxToPush = Mathf.Clamp(maxNotificationStack - notificationList.childCount, 0, maxNotificationStack);
        for (int i = 0; i < maxToPush; i++)
        {
            if (pendingNotifications.Count <= 0)
            {
                break;
            }

            PushNotification(pendingNotifications[0]);
            pendingNotifications.RemoveAt(0);
        }
    }

    private IEnumerator OnNotificationLifeTimeEnds(PushNotificationEntry notificationEntry) 
    {
        yield return new WaitForSeconds(notificationLifeTime);
        RemoveNotification(notificationEntry);
        TryPushPendingNotifications();
    }
    #endregion

    private void OnEnable()
    {
        dismissButton.onClick.AddListener(DismissAllNotifications);
        pendingNotifications.Clear();
    }

    private void OnDisable()
    {
        dismissButton.onClick.RemoveAllListeners();
        DismissAllNotifications();
    }
}
