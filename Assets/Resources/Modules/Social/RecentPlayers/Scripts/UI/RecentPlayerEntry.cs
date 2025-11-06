// Copyright (c) 2025 - 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;

public class RecentPlayerEntry : MonoBehaviour
{
    [SerializeField] private AccelByteWarsAsyncImage playerImage;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerStatus;
    [SerializeField] private TMP_Text playerSessionStatus;

    public void Init(string name, string status, string sessionStatus, string avatarUrl)
    {
        playerImage.LoadImage(avatarUrl);
        playerName.SetText(name);
        playerStatus.SetText(status);
        playerSessionStatus.SetText(sessionStatus);
    }

    public Sprite GetCurrentImage()
    {
        return playerImage?.GetCurrentImage();
    }
}
