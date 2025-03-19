// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Threading.Tasks;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine.Analytics;

public class MatchmakingSessionDSWrapperServer: MatchmakingSessionWrapper
{

#if UNITY_SERVER
    private ServerMatchmakingV2 matchmakingV2Server;
    private ServerDSHub serverDSHub;
    private ServerSession serverSession;
    private List<SessionV2MemberData> members = new List<SessionV2MemberData>();
    private bool isGameStarted;

    private void Awake()
    {
        base.Awake();
        matchmakingV2Server = AccelByteSDK.GetServerRegistry().GetApi().GetMatchmakingV2();
        serverDSHub = AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
        serverSession = AccelByteSDK.GetServerRegistry().GetApi().GetSession();
    }

    private void Start()
    {
        GameManager.Instance.OnRejectBackfill += () => { isGameStarted = true; };
        GameManager.Instance.OnGameStateIsNone += () => { isGameStarted = false; };
    }

    #region GameServerNotification

    public void BackFillProposal()
    {
        serverDSHub.MatchmakingV2BackfillProposalReceived += result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"BackFillProposal");

                if (!isGameStarted)
                {
                    OnBackfillProposalReceived(result.Value, isGameStarted);
                    BytewarsLogger.Log($"Start back-filling process {result.Value.matchSessionId}");
                }
                else
                {
                    OnBackfillProposalRejected(result.Value);
                }
            }
            else
            {
                BytewarsLogger.LogWarning($"BackFillProposal {result.Error.Message}");
            }
        };
    }

    private void OnBackfillProposalReceived(MatchmakingV2BackfillProposalNotification proposal, bool isStopBackfilling)
    {
        AcceptBackfillProposalOptionalParams param = new AcceptBackfillProposalOptionalParams()
        {
            StopBackfilling = isStopBackfilling
        };
        matchmakingV2Server.AcceptBackfillProposal(proposal, param, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Back-filling accepted {!isStopBackfilling}");
            }
        });
    }

    private void OnBackfillProposalRejected(MatchmakingV2BackfillProposalNotification proposal)
    {
        matchmakingV2Server.RejectBackfillProposal(proposal, true, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Back-filling rejected - Game already started");
            }
        });
    }

    public void OnServerSessionUpdate()
    {
        serverDSHub.GameSessionV2MemberChanged += result =>
        {
            if(!result.IsError)
            {
                members.Clear();
                members.AddRange(result.Value.members);
                BytewarsLogger.Log(result.Value.ToJsonString());
            }
            else
            {
                BytewarsLogger.LogWarning($"{result.Error.Message}");
            }
        };
    }
    
    #endregion GameServerNotification

#endif
}