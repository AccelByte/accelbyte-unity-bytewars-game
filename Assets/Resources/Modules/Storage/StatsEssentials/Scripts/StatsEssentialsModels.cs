// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.ObjectModel;
using System.Linq;

public class StatsEssentialsModels
{
    public class GameStatsData
    {
        public struct GameStatsModel
        {
            public string StatCode;
            public string DisplayName;

            public GameStatsModel(string codeName, string displayName)
            {
                StatCode = codeName;
                DisplayName = displayName;
            }
        }

        public InGameMode GameMode { get; private set; }
        public GameStatsModel HighestScoreStats { get; private set; }
        public GameStatsModel TotalScoreStats { get; private set; }
        public GameStatsModel MatchesPlayedStats { get; private set; }
        public GameStatsModel MatchesWonStats { get; private set; }
        public GameStatsModel KillCountStats { get; private set; }
        public GameStatsModel DeathStats { get; private set; }
        public ReadOnlyCollection<string> StatsCodes { get; }
        public ReadOnlyCollection<GameStatsModel> StatsModels { get; }

        public GameStatsData(InGameMode gameMode)
        {
            GameMode = gameMode;

            string gameModeSuffx = "unknown";
            switch(GameMode)
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

            HighestScoreStats = new($"unity-highestscore-{gameModeSuffx}", "Highest Score");
            TotalScoreStats = new($"unity-totalscore-{gameModeSuffx}", "Total Score");
            MatchesPlayedStats = new($"unity-matchesplayed-{gameModeSuffx}", "Matches Played");
            MatchesWonStats = new($"unity-matcheswon-{gameModeSuffx}", "Matches Won");
            KillCountStats = new($"unity-killcount-{gameModeSuffx}", "Kill Count");
            DeathStats = new($"unity-deaths-{gameModeSuffx}", "Deaths");

            StatsModels = new ReadOnlyCollection<GameStatsModel>(new[]
            {
                HighestScoreStats,
                TotalScoreStats,
                MatchesPlayedStats,
                MatchesWonStats,
                KillCountStats,
                DeathStats
            });

            StatsCodes = new ReadOnlyCollection<string>(StatsModels.Select(model => model.StatCode).ToList());
        }
    }

    public readonly static GameStatsData SinglePlayerStatsData = new(InGameMode.SinglePlayer);
    public readonly static GameStatsData EliminationStatsData = new(InGameMode.CreateMatchElimination);
    public readonly static GameStatsData TeamDeathmatchStatsData = new(InGameMode.CreateMatchTeamDeathmatch);

    public static GameStatsData GetGameStatsDataByGameMode(InGameMode gameMode)
    {
        switch (gameMode)
        {
            case InGameMode.SinglePlayer:
            case InGameMode.LocalElimination:
            case InGameMode.LocalTeamDeathmatch:
                return SinglePlayerStatsData;
            case InGameMode.MatchmakingElimination:
            case InGameMode.CreateMatchElimination:
                return EliminationStatsData;
            case InGameMode.MatchmakingTeamDeathmatch:
            case InGameMode.CreateMatchTeamDeathmatch:
                return TeamDeathmatchStatsData;
            default:
                return null;
        }
    }
}
