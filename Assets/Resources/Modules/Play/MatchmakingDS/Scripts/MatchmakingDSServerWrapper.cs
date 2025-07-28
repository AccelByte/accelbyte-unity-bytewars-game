// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;

public class MatchmakingDSServerWrapper: MatchmakingEssentialsWrapper
{
#if UNITY_SERVER
    protected override void Awake()
    {
        base.Awake();

        ServerDSHub.MatchmakingV2BackfillProposalReceived += OnBackfillProposalReceived;
    }

    private void OnBackfillProposalReceived(Result<MatchmakingV2BackfillProposalNotification> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"Failed to handle backfill proposal. Error {result.Error.Code}: {result.Error.Message}");
            return;
        }

        // If gameplay is not yet started, accept the backfill proposal.
        if (GameManager.Instance.InGameState == InGameState.None)
        {
            AcceptBackfillProposal(proposal: result.Value);
        }
        // Else, reject the proposal.
        else
        {
            RejectBackfillProposal(proposal: result.Value, stopBackfilling: true);
        }
    }

    private void AcceptBackfillProposal(MatchmakingV2BackfillProposalNotification proposal)
    {
        ServerMatchmaking.AcceptBackfillProposal(proposal, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to accept backfill proposal. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to accept backfill proposal. Session ID: {result.Value.id}. Backfill Ticket ID: {result.Value.backfillTicketId}");
            }
        });
    }

    private void RejectBackfillProposal(MatchmakingV2BackfillProposalNotification proposal, bool stopBackfilling)
    {
        ServerMatchmaking.RejectBackfillProposal(proposal, stopBackfilling, (result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to reject backfill proposal. Error {result.Error.Code}: {result.Error.Message}");
            }
            else
            {
                BytewarsLogger.Log($"Success to reject backfill proposal. Is stop backfilling: {stopBackfilling}");
            }
        });
    }
#endif
}
