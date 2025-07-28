// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Api.Interface;
using AccelByte.Core;
using AccelByte.Models;
using UnityEditor;
using UnityEngine;
using static ChallengeEssentialsModels;

public class ChallengeEssentialsWrapper : MonoBehaviour
{
    // AGS Game SDK references
    private IClientChallenge challenge;
    private Items items;

    private Dictionary<string /*sku*/, ItemInfo /*info*/> cachedRewardItemInfos = new();

    private void Awake()
    {
        challenge = AccelByteSDK.GetClientRegistry().GetApi().GetChallenge();
        items = AccelByteSDK.GetClientRegistry().GetApi().GetItems();
    }

    public void ClaimChallengeGoalRewards(
        List<string> rewardIDs, 
        ResultCallback onComplete)
    {
        challenge.ClaimReward(rewardIDs.ToArray(), (Result<UserReward[]> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to claim challenge rewards. Error {result.Error}: {result.Error.Message}");
                onComplete?.Invoke(Result.CreateError(result.Error));
                return;
            }

            if (result.Value.Length <= 0)
            {
                BytewarsLogger.LogWarning("Failed to claim challenge rewards. No claimable reward found.");
                onComplete?.Invoke(Result.CreateError(ErrorCode.NotFound, EmptyClaimableChallengeRewardMessage));
                return;
            }

            BytewarsLogger.Log("Success to claim challenge rewards.");
            onComplete?.Invoke(Result.CreateOk());
        });
    }

    public void GetChallengeByPeriod(
        ChallengeRotation period,
        ResultCallback<ChallengeResponseInfo> onComplete)
    {
        challenge.GetChallenges((Result<ChallengeResponse> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to get challenge. Error {result.Error.Code}: {result.Error.Message}");
                onComplete?.Invoke(Result<ChallengeResponseInfo>.CreateError(result.Error));
                return;
            }

            foreach (ChallengeResponseInfo challengeInfo in result.Value.Data)
            {
                // Skip inactive challenge.
                if (challengeInfo.Status.ToLower() == ChallengeStatus.Retired.ToString().ToLower())
                {
                    continue;
                }

                // Challenge codes in Byte Wars use the <engine>-<period> format, e.g., unity-alltime or unity-weekly.
                if (challengeInfo.Code.Contains("unity", System.StringComparison.OrdinalIgnoreCase) && 
                    challengeInfo.Rotation.ToLower() == period.ToString().ToLower())
                {
                    BytewarsLogger.Log($"Success to get challenge with {period} period. Challenge code: {challengeInfo.Code}");
                    onComplete?.Invoke(Result<ChallengeResponseInfo>.CreateOk(challengeInfo));
                    return;
                }
            }

            BytewarsLogger.LogWarning($"Failed to get challenge. No challenge found with {period} period.");
            onComplete?.Invoke(Result<ChallengeResponseInfo>.CreateError(ErrorCode.NotFound, EmptyChallengeMessage));
        });
    }

    public void GetChallengeGoalList(
        ChallengeResponseInfo challengeInfo,
        ResultCallback<List<ChallengeGoalData>> onComplete,
        int rotationIndex = 0)
    {
        // Request to evaluate to update challenge goals progresses.
        challenge.EvaluateChallengeProgress((Result evaluateResult) =>
        {
            if (evaluateResult.IsError)
            {
                OnGetChallengeGoalListComplete(false, evaluateResult.Error, null, onComplete);
                return;
            }

            // Get the goal list and their progress.
            challenge.GetChallengeProgress(
                challengeInfo.Code,
                rotationIndex, 
                (Result<GoalProgressionResponse> progressResult) =>
                {
                    if (progressResult.IsError)
                    {
                        OnGetChallengeGoalListComplete(false, progressResult.Error, null, onComplete);
                        return;
                    }

                    // Construct new goal object and add it to the list.
                    List<ChallengeGoalData> goals = new();
                    foreach(GoalProgressionInfo progress in progressResult.Value.Data)
                    {
                        goals.Add(new()
                        {
                            Meta = progress.Goal,
                            Progress = progress,
                            Rewards = new(),
                            EndDateTime =
                                challengeInfo.Rotation.ToLower() == ChallengeRotation.None.ToString().ToLower() ?
                                string.Empty : progressResult.Value.Meta.Period.EndTime
                        });
                    }

                    // Query reward item information for all goals.
                    QueryRewardItemsInformation(goals, (Result<List<ChallengeGoalData>> queryResult) =>
                    {
                        // Operation is complete, return the result.
                        OnGetChallengeGoalListComplete(queryResult.IsError, queryResult.Error, queryResult.Value, onComplete);
                    });
                });
        });
    }

    private void QueryRewardItemsInformation(
        List<ChallengeGoalData> goals,
        ResultCallback<List<ChallengeGoalData>> onComplete)
    {
        // Collect reward item SKUs to query.
        List<string> rewardItemSkusToQuery = goals
            .SelectMany(x => x.Meta.Rewards)
            .Select(x => x.ItemId).ToList();

        // Return success if all reward items are already cached.
        rewardItemSkusToQuery = rewardItemSkusToQuery.Except(cachedRewardItemInfos.Keys).ToList();
        if (rewardItemSkusToQuery.Count <= 0)
        {
            OnQueryRewardItemsInformationComplete(Result.CreateOk(), goals, onComplete);
            return;
        }

        // Query reward items information by SKUs recursively.
        cachedRewardItemInfos.Clear();
        QueryRewardItemsBySkusRecursively(rewardItemSkusToQuery, goals, onComplete);
    }

    private void QueryRewardItemsBySkusRecursively(
        List<string> itemSkusToQuery,
        List<ChallengeGoalData> goals,
        ResultCallback<List<ChallengeGoalData>> onComplete)
    {
        if (itemSkusToQuery.Count <= 0)
        {
            BytewarsLogger.Log("Success to query reward items info by SKUs.");
            OnQueryRewardItemsInformationComplete(Result.CreateOk(), goals, onComplete);
            return;
        }

        string currentSku = itemSkusToQuery[0];
        itemSkusToQuery.RemoveAt(0);

        items.GetItemBySku(
            currentSku,
            string.Empty,
            string.Empty,
            (Result<ItemInfo> result) =>
            {
                if (result.IsError)
                {
                    BytewarsLogger.LogWarning(
                        $"Failed to query reward items by SKU '{currentSku}'. " +
                        $"Error {result.Error.Code}: {result.Error.Message}");
                    onComplete?.Invoke(Result<List<ChallengeGoalData>>.CreateError(result.Error));
                    return;
                }

                // Store the info to the cache
                cachedRewardItemInfos.Add(result.Value.sku, result.Value);

                // Continue with the next SKU
                QueryRewardItemsBySkusRecursively(itemSkusToQuery, goals, onComplete);
            });
    }

    private void OnQueryRewardItemsInformationComplete(
        Result queryResult,
        List<ChallengeGoalData> goals,
        ResultCallback<List<ChallengeGoalData>> onComplete)
    {
        if (queryResult.IsError)
        {
            BytewarsLogger.LogWarning(
                $"Failed to query reward items info. " +
                $"Error {queryResult.Error.Code}: {queryResult.Error.Message}");
            onComplete?.Invoke(Result<List<ChallengeGoalData>>.CreateError(queryResult.Error));
            return;
        }

        // Construct goal reward data based on queried information.
        foreach (ChallengeGoalData goal in goals)
        {
            foreach (ChallengeReward reward in goal.Meta.Rewards)
            {
                cachedRewardItemInfos.TryGetValue(reward.ItemId, out ItemInfo rewardInfo);
                goal.Rewards.Add(new()
                {
                    Reward = reward,
                    ItemInfo = rewardInfo
                });
            }
        }

        BytewarsLogger.Log("Success to query reward items info.");
        onComplete?.Invoke(Result<List<ChallengeGoalData>>.CreateOk(goals));
    }

    private void OnGetChallengeGoalListComplete(
        bool isError,
        Error error,
        List<ChallengeGoalData> goals,
        ResultCallback<List<ChallengeGoalData>> onComplete)
    {
        if (isError)
        {
            BytewarsLogger.LogWarning($"Failed to get challenge goal list. Error {error.Code}: {error.Message}");
            onComplete?.Invoke(Result<List<ChallengeGoalData>>.CreateError(error));
            return;
        }

        // Sort the result based on the unclaimed rewards and completion status.
        goals.Sort((goal1, goal2) =>
        {
            // Goals with unclaimed rewards come first.
            bool goal1HasUnclaimed = (goal1.Progress.ToClaimRewards?.Length ?? 0) > 0;
            bool goal2HasUnclaimed = (goal2.Progress.ToClaimRewards?.Length ?? 0) > 0;
            if (goal1HasUnclaimed != goal2HasUnclaimed)
            {
                return goal1HasUnclaimed ? -1 : 1;
            }

            // Completed goals come before others.
            bool goal1Completed = goal1.Progress.Status.ToLower() == ChallengeGoalProgressStatus.Completed.ToString().ToLower();
            bool goal2Completed = goal2.Progress.Status.ToLower() == ChallengeGoalProgressStatus.Completed.ToString().ToLower();
            if (goal1Completed != goal2Completed)
            {
                return goal1Completed ? -1 : 1;
            }

            return 0;
        });

        BytewarsLogger.Log($"Success to get challenge goal list.");
        onComplete?.Invoke(Result<List<ChallengeGoalData>>.CreateOk(goals));
    }
}
