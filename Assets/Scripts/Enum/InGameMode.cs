public enum InGameMode
{
    SinglePlayerGameMode,
    Local2PlayerGameMode,
    Local3PlayerGameMode,
    Local4PlayerGameMode,
    Local4PlayerDeathMatchGameMode,
    OnlineEliminationGameMode,
    OnlineDeathMatchGameMode,
    CreateMatchEliminationGameMode,
    CreateMatchDeathMatchGameMode,
    None//None should always last, because this enum is actually an index to GameManager.availableInGameMode
}
