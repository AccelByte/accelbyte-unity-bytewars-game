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

public class ChallengeEntry : MonoBehaviour
{
    [SerializeField] private Transform rewardPanel;
    [SerializeField] private ChallengeGoalRewardEntry rewardEntryPrefab;

    [SerializeField] private Toggle statusCheckBox;
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private ButtonAnimation claimButton;

    private ChallengeEssentialsWrapper challengeWrapper;

    private void OnEnable()
    {
        challengeWrapper ??= TutorialModuleManager.Instance.GetModuleClass<ChallengeEssentialsWrapper>();
    }

    public void Setup(ChallengeGoalData goal)
    {
        ChallengeGoalMeta meta = goal.Meta;
        GoalProgressionInfo progress = goal.Progress;
        bool isRewardClaimed = (progress.ToClaimRewards?.Length ?? 0) <= 0;
        bool isNotStarted = progress.Status.ToLower() == ChallengeGoalProgressStatus.Not_Started.ToString().ToLower();
        bool isCompleted = progress.Status.ToLower() == ChallengeGoalProgressStatus.Completed.ToString().ToLower();

        // Display basic information.
        goalText.text = meta.Name;
        remainingTimeText.text = goal.EndTimeDuration;
        statusCheckBox.isOn = isCompleted;

        // Display rewards
        rewardPanel.DestroyAllChildren();
        foreach(ChallengeGoalRewardData reward in goal.Rewards)
        {
            Instantiate(rewardEntryPrefab, rewardPanel).GetComponent<ChallengeGoalRewardEntry>().Setup(reward);
        }

        // Setup claim reward button.
        claimButton.text.text = isRewardClaimed ? ClaimedChallengeRewardLabel : ClaimableChallengeRewardLabel;
        claimButton.button.enabled = !isRewardClaimed;
        claimButton.button.onClick.RemoveAllListeners();
        claimButton.button.onClick.AddListener(() => OnClaimButtonClicked(goal));

        /* Select the progress with the highest progress value, as Byte Wars displays only one.
	     * If there is no player progress, set the default goal progress value from the requirement group.
	     * Else, use the actual player progress value. */
        int currentProgress = 0, targetProgress = 0;
        if (isNotStarted)
        {
            ChallengePredicate predicate = progress.Goal.RequirementGroups
                .SelectMany(x => x.Predicates)
                .OrderByDescending(x => x.TargetValue)
                .FirstOrDefault();
            targetProgress = (int?)(predicate?.TargetValue) ?? 0;
        }
        else
        {
            ChallengeRequirementProgressionResponse requirement = progress.RequirementProgressions
                .OrderByDescending(x => x.CurrentValue)
                .FirstOrDefault();
            currentProgress = (int?)(requirement?.CurrentValue) ?? 0;
            targetProgress = (int?)(requirement?.TargetValue) ?? 0;
        }
        progressText.text = $"{currentProgress}/{targetProgress}";

        // Display progress if not completed, otherwise show the claim reward button.
        progressText.gameObject.SetActive(!isCompleted);
        claimButton.gameObject.SetActive(isCompleted);
    }

    private void OnClaimButtonClicked(ChallengeGoalData goal)
    {
        // Abort if no rewards to claim.
        if (goal.Progress.ToClaimRewards == null)
        {
            MenuManager.Instance.ShowInfo(EmptyClaimableChallengeRewardMessage, "Error");
            return;
        }

        // Collect claimable reward IDs.
        List<string> claimableRewardIDs = goal.Progress.ToClaimRewards.Select(x => x.Id).ToList();

        // Claim rewards.
        claimButton.button.enabled = false;
        claimButton.text.text = ClaimingChallengeRewardLabel;
        challengeWrapper.ClaimChallengeGoalRewards(claimableRewardIDs, (Result result) =>
        {
            claimButton.button.enabled = result.IsError;
            claimButton.text.text = result.IsError ? ClaimableChallengeRewardLabel : ClaimedChallengeRewardLabel;

            if (result.IsError)
            {
                MenuManager.Instance.ShowInfo(result.Error.Message, "Error");
            }
        });
    }
}
