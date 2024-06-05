// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindFriendsEntryHandler : MonoBehaviour, IFriendsEntryHandler
{
    [SerializeField] private Image friendImage;
    [SerializeField] private TMP_Text friendName;
    [SerializeField] private TMP_Text friendStatus;
    [SerializeField] private Button sendInviteButton;

    public string UserId { get; set; } = string.Empty;

    public Image FriendImage => friendImage;
    
    public TMP_Text FriendName => friendName;
    
    public TMP_Text FriendStatus => friendStatus;
    
    public Button SendInviteButton => sendInviteButton;
}
