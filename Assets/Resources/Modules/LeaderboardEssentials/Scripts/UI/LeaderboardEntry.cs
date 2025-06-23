// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text scoreText;

    public void SetRankingDetails(string userId, int rank, string displayName, float score)
    {
        // If display name is null or empty, set to default format: Player-{first 5 char of user ID}
        displayNameText.text = string.IsNullOrEmpty(displayName) ? $"Player-{userId[..5]}" : displayName;
        rankText.text = $"{rank}";
        scoreText.text = $"{score}";
    }

    public void Reset()
    {
        displayNameText.text = LeaderboardEssentialsModels.UnrankedMessage;
        rankText.text = "?";
        scoreText.text = "";
    }
}
