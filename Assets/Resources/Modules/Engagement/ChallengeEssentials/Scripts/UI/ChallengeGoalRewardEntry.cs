// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using static ChallengeEssentialsModels;

public class ChallengeGoalRewardEntry : MonoBehaviour
{
    [SerializeField] private AccelByteWarsAsyncImage rewardImage;
    [SerializeField] private TMP_Text rewardValueText;

    public void Setup(ChallengeGoalRewardData data)
    {
        rewardValueText.text = data.Reward.Quantity.ToString();
        if (data.ItemInfo.images?.Length > 0)
        {
            // Use the first image to show the reward icon.
            rewardImage.LoadImage(data.ItemInfo.images[0].smallImageUrl);
        }
    }
}
