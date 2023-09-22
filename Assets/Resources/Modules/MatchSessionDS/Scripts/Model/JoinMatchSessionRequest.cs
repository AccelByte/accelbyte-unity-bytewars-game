public struct JoinMatchSessionRequest
{
    public JoinMatchSessionRequest(string matchSessionId, InGameMode gameMode)
    {
        MatchSessionId = matchSessionId;
        GameMode = gameMode;
    }
    public string MatchSessionId;
    public InGameMode GameMode;
}