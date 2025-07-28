// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using AccelByte.Models;

public class ChallengeEssentialsModels
{
    public const string EmptyChallengeMessage = "No Challenge Found";
    public const string EmptyClaimableChallengeRewardMessage = "No Claimable Challenge Reward Found";
    public const string ClaimedChallengeRewardLabel = "Claimed";
    public const string ClaimableChallengeRewardLabel = "Claim";
    public const string ClaimingChallengeRewardLabel = "Claiming";
    public const string AllTimeChallengeTitleLabel = "All Time Challenges";
    public const string PeriodicChallengeTitleLabel = "{0} Challenges";

    [JsonConverter(typeof(StringEnumConverter)), Serializable]
    public enum ChallengeRotation
    {
        [Description("NONE"), EnumMember(Value = "NONE")]
        None,

        [Description("DAILY"), EnumMember(Value = "DAILY")]
        Daily,

        [Description("WEEKLY"), EnumMember(Value = "WEEKLY")]
        Weekly,

        [Description("MONTHLY"), EnumMember(Value = "MONTHLY")]
        Monthly,

        [Description("CUSTOM"), EnumMember(Value = "CUSTOM")]
        Custom
    }

    [JsonConverter(typeof(StringEnumConverter)), Serializable]
    public enum ChallengeGoalProgressStatus
    {
        [Description("NONE"), EnumMember(Value = "NONE")]
        None,

        [Description("ACTIVE"), EnumMember(Value = "ACTIVE")]
        Active,

        [Description("COMPLETED"), EnumMember(Value = "COMPLETED")]
        Completed,

        [Description("RETIRED"), EnumMember(Value = "RETIRED")]
        Retired,

        [Description("NOT_STARTED"), EnumMember(Value = "NOT_STARTED")]
        Not_Started
    };

    [Serializable]
    public struct ChallengeGoalData
    {
        public ChallengeGoalMeta Meta;
        public GoalProgressionInfo Progress;
        public List<ChallengeGoalRewardData> Rewards;
        public string EndDateTime;

        public string EndTimeDuration
        {
            get
            {
                if (!DateTime.TryParse(
                    EndDateTime, 
                    null, 
                    System.Globalization.DateTimeStyles.AdjustToUniversal, 
                    out DateTime parsedEndDateTime))
                {
                    return string.Empty;
                }

                TimeSpan duration = parsedEndDateTime - DateTime.UtcNow;
                if (duration.TotalMinutes < 1)
                {
                    return "< 1m";
                }

                string result = string.Empty;
                if (duration.Days > 0)
                {
                    result += $"{duration.Days}d ";
                }
                if (duration.Hours > 0)
                {
                    result += $"{duration.Hours}h ";
                }
                if (duration.Minutes > 0)
                {
                    result += $"{duration.Minutes}m";
                }

                return result.Trim();
            }
        }
    }

    [Serializable]
    public struct ChallengeGoalRewardData
    {
        public ChallengeReward Reward;
        public ItemInfo ItemInfo;
    }
}
