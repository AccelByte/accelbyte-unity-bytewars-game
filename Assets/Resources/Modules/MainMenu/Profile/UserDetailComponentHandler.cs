// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserDetailComponentHandler : MonoBehaviour
{
    [SerializeField] private Image Avatar;
    [SerializeField] private TMP_Text DisplayName;
    [SerializeField] private TMP_Text UserId;
    [SerializeField] private TMP_Text LoginPlatform;

    void OnEnable()
    {
        if (GameData.CachedPlayerState != null)
        {
            DisplayName.text = GameData.CachedPlayerState.PlayerName;
            UserId.text = GameData.CachedPlayerState.PlayerId;
            LoginPlatform.text = $"Platform: {GameData.CachedPlayerState.PlatformId}";
        }
    }
}
