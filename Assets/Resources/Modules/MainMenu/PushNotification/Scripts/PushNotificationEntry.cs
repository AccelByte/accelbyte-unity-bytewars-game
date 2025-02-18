// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;

public class PushNotificationEntry : MonoBehaviour
{
    [SerializeField] private AccelByteWarsAsyncImage icon;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private TMP_Text message;
    [SerializeField] private ButtonAnimation[] actionButtons;

    private PushNotificationModel cachedNotification;
    public PushNotificationModel CachedNotification => cachedNotification;

    public void Init(PushNotificationModel notification) 
    {
        cachedNotification = notification;

        // Set icon image.
        if (string.IsNullOrEmpty(notification.IconUrl))
        {
            icon.ResetImage();
            icon.gameObject.SetActive(notification.UseDefaultIconOnEmpty);
        }
        else
        {
            icon.gameObject.SetActive(true);
            icon.LoadImage(notification.IconUrl);
        }

        // Set notif message.
        message.text = notification.Message;

        // Reset action buttons.
        foreach(ButtonAnimation actionButton in actionButtons) 
        {
            actionButton.button.onClick.RemoveAllListeners();
            actionButton.gameObject.SetActive(false);
        }

        // Instantiate action buttons.
        if (notification.ActionButtonTexts != null) 
        {
            int buttonIndex = 0;
            foreach (string actionButtonText in notification.ActionButtonTexts)
            {
                PushNotificationActionResult actionResult = (PushNotificationActionResult)buttonIndex;
                actionButtons[buttonIndex].text.text = actionButtonText;
                actionButtons[buttonIndex].gameObject.SetActive(true);
                actionButtons[buttonIndex].button.onClick.AddListener(() =>
                {
                    SubmitActionResult(notification, actionResult);
                });
                buttonIndex++;
            }
        }
    }

    private void SubmitActionResult(PushNotificationModel notification, PushNotificationActionResult actionResult) 
    {
        notification.ActionButtonCallback?.Invoke(actionResult);
        MenuManager.Instance.RemoveNotification(this);
    }
}
