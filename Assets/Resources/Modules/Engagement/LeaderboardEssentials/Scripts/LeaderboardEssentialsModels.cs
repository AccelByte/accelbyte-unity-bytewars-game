// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;

public class LeaderboardEssentialsModels
{
    public static readonly int QueryRankingsLimit = 10;
    public static readonly string RankedMessage = "Your Rank";
    public static readonly string UnrankedMessage = "You Are Unranked";

    public static string GetLeaderboardCodeByGameMode(InGameMode gameMode)
    {
        string gameModeSuffx = "unknown";
        switch (gameMode)
        {
            case InGameMode.SinglePlayer:
            case InGameMode.LocalElimination:
            case InGameMode.LocalTeamDeathmatch:
                gameModeSuffx = "singleplayer";
                break;
            case InGameMode.MatchmakingElimination:
            case InGameMode.CreateMatchElimination:
                gameModeSuffx = "elimination";
                break;
            case InGameMode.MatchmakingTeamDeathmatch:
            case InGameMode.CreateMatchTeamDeathmatch:
                gameModeSuffx = "teamdeathmatch";
                break;
        }
        return $"board-unity-highestscore-{gameModeSuffx}";
    }

    public static string GetFormattedLeaderboardPeriod(StatisticCycleType period) => $"unity-{period.ToString()}".ToLower();
}
