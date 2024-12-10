// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestsEntryHandler : MonoBehaviour, IFriendsEntryHandler
{
    [SerializeField] private Image friendImage;
    [SerializeField] private TMP_Text friendName;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button acceptButton;

    public string UserId { get; set; } = string.Empty;

    public Image FriendImage => friendImage;
    
    public TMP_Text FriendName => friendName;
    
    public Button RejectButton => rejectButton;
    
    public Button AcceptButton => acceptButton;
}
