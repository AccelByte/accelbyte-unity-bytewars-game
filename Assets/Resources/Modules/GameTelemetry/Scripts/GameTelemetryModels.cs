// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public class GameTelemetryModels
{
    public static string GetFormattedDeathSource(
        GameEntityDestroyReason destroyReason,
        PlayerState playerState,
        MissileState missileState,
        PlanetState planetState)
    {
        string result = "Unknown";

        // Return in Type:EntityId format (e.g. Player:12345)
        switch (destroyReason)
        {
            case GameEntityDestroyReason.PlayerHit:
                result = $"{GameEntityType.Player.ToString()}:{playerState?.EntityId}";
                break;
            case GameEntityDestroyReason.PlanetHit:
                result = $"{GameEntityType.Planet.ToString()}:{planetState?.EntityId}";
                break;
            case GameEntityDestroyReason.MissileHit:
                result = $"{GameEntityType.Missile.ToString()}:{missileState?.EntityId}";
                break;
        }
        return result;
    }

    public static string GetFormattedGameMode(InGameMode gameMode) 
    {
        string result = "None";

        // Return in SessionType:GameMode format.
        switch (gameMode) 
        {
            case InGameMode.SinglePlayer:
                result = "Offline:SinglePlayer";
                break;
            case InGameMode.LocalElimination:
                result = "Offline:Elimination";
                break;
            case InGameMode.LocalTeamDeathmatch:
                result = "Offline:TeamDeathmatch";
                break;
            case InGameMode.MatchmakingElimination:
                result = "Matchmaking:Elimination";
                break;
            case InGameMode.MatchmakingTeamDeathmatch:
                result = "Matchmaking:TeamDeathmatch";
                break;
            case InGameMode.CreateMatchElimination:
                result = "CreateMatch:Elimintation";
                break;
            case InGameMode.CreateMatchTeamDeathmatch:
                result = "CreateMatch:TeamDeathmatch";
                break;
        }
        return result;
    }
}
