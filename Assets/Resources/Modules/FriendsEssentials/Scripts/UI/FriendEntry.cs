// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendEntry : MonoBehaviour
{
    [SerializeField] private FriendEntryView entryView = FriendEntryView.Default;
    [SerializeField] private Image friendImage;
    [SerializeField] private TMP_Text friendName;
    [SerializeField] private TMP_Text friendStatus;
    [SerializeField] private Button sendInviteButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;
    
    public enum FriendEntryView
    {
        Default,
        Searched,
        PendingInbound,
        PendingOutbound
    }

    public FriendEntryView EntryView
    {
        get => entryView;
        set
        {
            entryView = value;
            friendStatus.gameObject.SetActive(entryView == FriendEntryView.Searched);
            sendInviteButton.gameObject.SetActive(entryView == FriendEntryView.Searched);
            rejectButton.gameObject.SetActive(entryView == FriendEntryView.PendingInbound);
            acceptButton.gameObject.SetActive(entryView == FriendEntryView.PendingInbound);
            cancelButton.gameObject.SetActive(entryView == FriendEntryView.PendingOutbound);
        }
    }

    public string UserId { get; set; } = string.Empty;

    public Image FriendImage => friendImage;

    public TMP_Text FriendName => friendName;

    public TMP_Text FriendStatus => friendStatus;

    public Button CancelButton => cancelButton;

    public Button RejectButton => rejectButton;

    public Button AcceptButton => acceptButton;

    public Button SendInviteButton => sendInviteButton;

    private void OnValidate()
    {
        EntryView = entryView;
    }
}
