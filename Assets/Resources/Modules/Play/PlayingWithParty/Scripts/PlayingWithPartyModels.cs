// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public class PlayingWithPartyModels
{
    public static readonly string PartyMembersGameSessionStatusesKey = "party_member_game_session_statuses";

    public static readonly string PartyMatchmakingStartedMessage = "Party Matchmaking Started by Party Leader";
    public static readonly string PartyMatchmakingSuccessMessage = "Party Matchmaking Found, Joining Match";
    public static readonly string PartyMatchmakingFailedMessage = "Party Matchmaking failed";
    public static readonly string PartyMatchmakingCanceledMessage = "Party Matchmaking is canceled by party leader";
    public static readonly string PartyMatchmakingExpiredMessage = "Party Matchmaking expired";
    public static readonly string PartyMatchmakingSafeguardMessage = "Matchmaking with this game mode is not supported when in a party";

    public static readonly string JoinPartyGameSessionMessage = "Joining Party Leader Game Session";
    public static readonly string JoinPartyGameSessionFailedMessage = "Failed to Join Party Game Session";
    public static readonly string JoinPartyGameSessionCanceledMessage = "Party game session is canceled by party leader";
    public static readonly string JoinPartyGameSessionWaitServerMessage = "Joined Party Game Session. Waiting for Server";
    public static readonly string JoinPartyGameSessionServerErrorMessage = "Party Game Session failure. Cannot find game server.";
    public static readonly string JoinPartyGameSessionSafeguardMessage = "Cannot join session. Insufficient slots to join with party";

    public static readonly string PartyGameSessionLeaderSafeguardMessage = "Cannot play online session since party members are on other session";
    public static readonly string PartyGameSessionMemberSafeguardMessage = "Only party leader can start online session";
}
