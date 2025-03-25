// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine.UI;

public interface IFriendsEntryHandler
{
    public string UserId { get; set; }

    public Image FriendImage { get; }

    public TMP_Text FriendName { get; }
}
