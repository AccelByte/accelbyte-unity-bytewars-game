// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public class CustomMatchmakingModels
{
    public static readonly string CustomMatchmakerConfigKey = "CustomMatchmakingUrl";
    public static readonly InGameMode DefaultGameMode = InGameMode.OnlineEliminationGameMode;

    public static readonly string TravelingMessage = "Traveling to Server";
    public static readonly string FindingMatchMessage = "Finding Match";
    public static readonly string RequestMatchmakingMessage = "Requesting";
    public static readonly string CancelMatchmakingMessage = "Canceling";
    public static readonly string MatchmakingErrorMessage = "Connection failed. Make sure the matchmaking server is running, reachable, and the address and port is set properly.";
    public static readonly string MatchmakingInvalidPayloadErrorMessage = "Received invalid payload format from matchmaking server. Make sure you are running a compatible version.";

    public enum MatchmakerPayloadType
    {
        OnFindingMatch,
        OnMatchFound,
        OnMatchError,
        OnServerReady
    }

    public class MatchmakerPayload
    {
        public MatchmakerPayloadType type { get; set; }
        public string message { get; set; }
    }

    public static string GetMatchmakerUrl()
    {
        // Get from launch parameter first.
        string customMatchmakerUrl = TutorialModuleUtil.GetLaunchParamValue($"-{CustomMatchmakerConfigKey}=");

        // Get from config file if laucnh params are empty.
        if (ConfigurationReader.Config != null)
        {
            if (string.IsNullOrEmpty(customMatchmakerUrl))
            {
                customMatchmakerUrl = ConfigurationReader.Config.AMSModuleConfiguration.customMatchmakingUrl;
            }
        }

        // Parse localhost if any, because it is not supported by Unity's networking.
        return Utilities.TryParseLocalHostUrl(customMatchmakerUrl);
    }
}
