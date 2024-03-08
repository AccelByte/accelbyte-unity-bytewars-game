// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionP2PWrapper_Starter : MonoBehaviour
{
    private MatchmakingSessionWrapper matchmakingSessionWrapper;
    private string matchTicketId;
    private string matchSessionId;
    private SessionV2GameSession joinedGameSession = null;
    private readonly TutorialType tutorialType = TutorialType.MatchmakingWithP2P;
    private InGameMode inGameMode = InGameMode.None;
    protected internal event Action OnCancelMatchmakingComplete;
    protected internal event Action<string/*error message*/> OnError;
    protected internal event Action OnMatchFound;
    protected internal event Action OnStartingP2PConnection;
    private bool isEventsListened;
    private bool isCancelled;

    private void Start()
    {

    }

    private void OnEnable()
    {

    }

    protected internal void StartMatchmaking(string matchPool, InGameMode inGameMode)
    {
        BytewarsLogger.Log("[MatchmakingSessionP2PWrapper_Starter] peer to peer matchmaking is not implemented yet");
    }

    protected internal void CancelMatchmaking()
    {
        isCancelled = true;
        StopAllCoroutines();
        if (String.IsNullOrWhiteSpace(matchTicketId))
        {
            OnCancelMatchmakingComplete?.Invoke();
            Reset();
            return;
        }

        if (String.IsNullOrWhiteSpace(matchSessionId))
        {
            matchmakingSessionWrapper.CancelMatchmaking(matchTicketId);
        }
        else if (joinedGameSession != null)
        {
            matchmakingSessionWrapper.OnLeaveSessionCompleteEvent += OnLeaveSessionComplete;
            matchmakingSessionWrapper.LeaveSession(matchSessionId, tutorialType);
        }
        Reset();
    }

    private void OnLeaveSessionComplete(SessionResponsePayload payload)
    {
        if (payload.TutorialType == tutorialType)
        {
            matchmakingSessionWrapper.OnLeaveSessionCompleteEvent -= OnLeaveSessionComplete;
            OnCancelMatchmakingComplete?.Invoke();
        }
    }

    private void BindEventListener()
    {
        if (!isEventsListened)
        {
            matchmakingSessionWrapper.OnStartMatchmakingCompleteEvent += OnStartMatchmakingComplete;
            matchmakingSessionWrapper.OnCancelMatchmakingCompleteEvent += OnSessionWrapperCancelMatchmakingComplete;
            isEventsListened = true;
        }
    }

    private void UnbindEventListener()
    {
        matchmakingSessionWrapper.OnStartMatchmakingCompleteEvent -= OnStartMatchmakingComplete;
        matchmakingSessionWrapper.OnCancelMatchmakingCompleteEvent -= OnSessionWrapperCancelMatchmakingComplete;
    }

    private void OnSessionMatchFound(string matchSessionId)
    {

    }

    private void OnJoinedSessionEvent(SessionResponsePayload payload)
    {

    }

    private void StartP2PConnection(string currentUserId, SessionV2GameSession gameSession)
    {

    }

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {

    }

    private void OnSessionWrapperCancelMatchmakingComplete()
    {
        OnCancelMatchmakingComplete?.Invoke();
    }

    private void Reset()
    {
        matchTicketId = null;
        matchSessionId = null;
        joinedGameSession = null;
    }
}
