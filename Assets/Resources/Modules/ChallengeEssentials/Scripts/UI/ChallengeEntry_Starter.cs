// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChallengeEssentialsModels;

public class ChallengeEntry_Starter : MonoBehaviour
{
    [SerializeField] private Transform rewardPanel;
    [SerializeField] private ChallengeGoalRewardEntry rewardEntryPrefab;

    [SerializeField] private Toggle statusCheckBox;
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private ButtonAnimation claimButton;

    private void OnEnable()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here
}
