// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public class MatchmakingEssentialsModels
{
    public static readonly string MatchTicketIdAttributeKey = "ticketID";

    public static readonly string StartMatchmakingMessage = "Start Matchmaking";
    public static readonly string FindingMatchMessage = "Finding Match";
    public static readonly string CancelMatchmakingMessage = "Cancel Matchmaking";
    public static readonly string MatchmakingExpiredMessage = "Matchmaking Expired. Please Try Again";

    public enum MatchmakingMenuState
    {
        StartMatchmaking,
        FindingMatch,
        MatchFound,
        CancelMatchmaking,
        JoinMatchConfirmation,
        JoinMatch,
        JoinedMatch,
        LeaveMatch,
        RejectMatch,
        RequestingServer,
        Error
    };
}
