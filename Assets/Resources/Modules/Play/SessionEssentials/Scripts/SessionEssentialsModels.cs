// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public class SessionEssentialsModels
{
    public static readonly int StateChangeDelay = 2;
    public static readonly int JoinMatchConfirmationTimeout = 10;
    public static readonly int RequestingServerTimeout = 60;

    public static readonly string CreatingSessionMessage = "Creating Session";
    public static readonly string LeavingSessionMessage = "Leaving Session";
    public static readonly string InvalidSessionTypeMessage = "Invalid Session Type";
    public static readonly string InvalidSessionPaginationMessage = "Invalid session pagination.";

    public static readonly string CreatingMatchMessage = "Creating Match";
    public static readonly string JoiningMatchMessage = "Joining Match";
    public static readonly string MatchFoundMessage = "Match Found";
    public static readonly string MatchJoinedMessage = "Match Joined";
    public static readonly string JoinMatchConfirmationMessage = "Waiting For Player";
    public static readonly string AutoJoinSessionMessage = "Auto Join In: {0}";
    public static readonly string RejectingMatchMessage = "Rejecting Match";
    public static readonly string LeavingMatchMessage = "Leaving Match";

    public static readonly string RequestingServerMessage = "Requesting Server";
    public static readonly string RequestingServerTimerMessage = "Requesting Server Timeout: {0}";
    public static readonly string FailedToFindServerMessage = "Failed to Travel to the Game Server. Game Server Not Found. Please Try Again";
}
