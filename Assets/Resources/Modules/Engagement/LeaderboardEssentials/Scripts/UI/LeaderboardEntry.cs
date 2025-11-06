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
        displayNameText.text = displayName;
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
