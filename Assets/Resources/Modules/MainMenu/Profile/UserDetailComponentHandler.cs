// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;

public class UserDetailComponentHandler : MonoBehaviour
{
    [SerializeField] private AccelByteWarsAsyncImage Avatar;
    [SerializeField] private TMP_Text DisplayName;
    [SerializeField] private TMP_Text UserId;
    [SerializeField] private TMP_Text LoginPlatform;

    void OnEnable()
    {
        if (GameData.CachedPlayerState != null)
        {
            Avatar.LoadImage(GameData.CachedPlayerState.AvatarUrl);
            DisplayName.text = GameData.CachedPlayerState.PlayerName;
            UserId.text = $"User ID: {GameData.CachedPlayerState.PlayerId}";
            LoginPlatform.text = $"Platform: {GameData.CachedPlayerState.PlatformId.ToUpper()}";
        }
    }
}
