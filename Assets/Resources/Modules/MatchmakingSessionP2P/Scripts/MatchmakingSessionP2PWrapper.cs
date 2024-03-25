// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class MatchmakingSessionP2PWrapper : MonoBehaviour
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
    private Action<int> onStartMatchmakingCompleted;

    private void Start()
    {
        matchmakingSessionWrapper = TutorialModuleManager.Instance
            .GetModuleClass<MatchmakingSessionWrapper>();
        MatchmakingSessionServerTypeSelection.OnBackButtonCalled += UnbindEventListener;
    }

    private void OnEnable()
    {
        if (matchmakingSessionWrapper == null)
        {
            matchmakingSessionWrapper = TutorialModuleManager.Instance
                .GetModuleClass<MatchmakingSessionWrapper>();
        }
    }

    protected internal void StartMatchmaking(string matchPool, InGameMode inGameMode,
        Action<int> onStartMatchmakingCompleted)
    {
        this.onStartMatchmakingCompleted = onStartMatchmakingCompleted;
        Debug.Log($"start matchmaking match pool:{matchPool}");
        isCancelled = false;
        BindEventListener();
        this.inGameMode = inGameMode;
        ConnectionHandler.Initialization();
        var isLocal = ConnectionHandler.IsUsingLocalDS();
        Reset();
        matchmakingSessionWrapper.StartMatchmaking(matchPool, isLocal);
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
        Debug.Log($"Cancelling match ticket id:{matchTicketId}");
        matchmakingSessionWrapper.CancelMatchmaking(matchTicketId);
        if (joinedGameSession != null)
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
        matchmakingSessionWrapper.OnMatchmakingFoundEvent -= OnSessionMatchFound;
        matchmakingSessionWrapper.OnMatchFoundFallbackEvent -= OnSessionMatchFound;
        this.matchSessionId = matchSessionId;
        if (isCancelled)
        {
            return;
        }
        matchmakingSessionWrapper.OnJoinSessionCompleteEvent += OnJoinedSessionEvent;
        matchmakingSessionWrapper.JoinSession(matchSessionId, tutorialType);
        OnMatchFound?.Invoke();
    }

    private void OnJoinedSessionEvent(SessionResponsePayload payload)
    {
        if (payload.IsError)
        {
            OnError?.Invoke(payload.Result.Error.Message);
            matchmakingSessionWrapper.OnJoinSessionCompleteEvent -= OnJoinedSessionEvent;
            return;
        }
        if (payload.TutorialType == tutorialType)
        {
            joinedGameSession = payload.Result.Value;
            if (isCancelled)
            {
                BytewarsLogger.LogWarning("P2P matchmaking session joined event is called, but matchmaking is already cancelled");
            }
            else
            {
                var currentUserId = AccelByteSDK.GetClientRegistry().GetApi().session.UserId;
                //joinedGameSession.id is the same as matchSessionId
                SessionCache.SetJoinedSessionIdAndLeaderUserId(joinedGameSession.id, joinedGameSession.leaderId);
                UnbindEventListener();
                OnStartingP2PConnection?.Invoke();
                StartP2PConnection(currentUserId, joinedGameSession);
                matchmakingSessionWrapper.OnJoinSessionCompleteEvent -= OnJoinedSessionEvent;
            }
        }
        else
        {
            BytewarsLogger.LogWarning($"Unmatched TutorialType Received, expected:{tutorialType} received:{payload.TutorialType}");
        }
    }

    private void StartP2PConnection(string currentUserId, SessionV2GameSession gameSession)
    {
        var leaderUserId = gameSession.leaderId;
        if (!String.IsNullOrWhiteSpace(leaderUserId))
        {
            if (currentUserId.Equals(leaderUserId))
            {
                GameData.ServerSessionID = gameSession.id;
                P2PHelper.StartAsHost(inGameMode, gameSession.id);
            }
            else
            {
                P2PHelper.StartAsP2PClient(leaderUserId, inGameMode, gameSession.id);
            }
        }
        else
        {
            string errorMessage = $"GameSession.id {gameSession.id} has empty/null leader id: {gameSession.leaderId} ";
            OnError?.Invoke(errorMessage);
        }
    }

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (result.IsError)
        {
            OnError?.Invoke(result.Error.Message);
            return;
        }
        if (isCancelled)
        {
            return;
        }
        matchTicketId = result.Value.matchTicketId;
        onStartMatchmakingCompleted(result.Value.queueTime);
        matchmakingSessionWrapper.OnMatchmakingFoundEvent += OnSessionMatchFound;
        matchmakingSessionWrapper.GetMatchmakingTicketDetails(matchTicketId, true);
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
