// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;
using static MatchmakingFallback;

public class MatchmakingSessionDSWrapper_Starter : MatchmakingSessionWrapper
{
#if UNITY_SERVER
    private ServerMatchmakingV2 matchmakingV2Server;
    private ServerDSHub serverDSHub;
#endif
    private MatchmakingFallback _matchmakingFallback = new MatchmakingFallback();
    private string matchTicket;
    private bool isGameStarted;
    private string sessionId;
    private bool isSessionActive;
    private bool isOnJoinSession;
    private bool isDSUpdateError;
    private bool onJoinEvent;
    private bool onDSAvailable;
    private bool isFired = false;
    private const int queueTimeOffsetSec = 5;

    private static readonly TutorialType _tutorialType = TutorialType.MatchmakingWithDS;

    protected internal event Action OnStartMatchmakingFailed;
    protected internal event Action<string> OnStartMatchmakingSucceedEvent;
    protected internal event Action<SessionResponsePayload> OnMatchmakingJoinSessionCompleteEvent;
    protected internal event Action OnMatchmakingJoinSessionFailedEvent;
    protected internal event Action<SessionV2GameSession> OnDSAvailableEvent;
    protected internal event Action OnDSFailedRequestEvent;
    protected internal event Action OnSessionEnded;

    private void Awake()
    {
        base.Awake();
#if UNITY_SERVER
#endif

    }

    private void Start()
    {
#if UNITY_SERVER
#endif
    }

    protected internal void BindEventListener()
    {
        SetupMatchmakingEventListener(true, false);
        base.OnStartMatchmakingCompleteEvent += OnStartMatchmakingComplete;
        base.OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    protected internal void UnbindEventListener()
    {
        UnbindMatchmakingEventListener(true, false);
        base.OnStartMatchmakingCompleteEvent -= OnStartMatchmakingComplete;
        base.OnCancelMatchmakingCompleteEvent -= OnCancelMatchmakingComplete;
        isFired = false;
    }

    #region Fallback
    private void CheckMatchFoundNotificationFallback(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (!result.IsError)
        {
            Debug.Log(JsonUtility.ToJson(result.Value));
            StartCoroutine(StartFallbackTimer(result.Value.queueTime));
        }
        else
        {
            Debug.LogWarning($"{result.Error.Message}");
        }
    }

    private void CheckDSStatusUpdateNotificationFallback(Result<SessionV2GameJoinedNotification> result)
    {
        if (!result.IsError)
        {
            Debug.Log(JsonUtility.ToJson(result.Value));
            StartCoroutine(StartFallbackTimer(5));
        }
        else
        {
            Debug.LogWarning($"{result.Error.Message}");
        }
    }

    private IEnumerator StartFallbackTimer(int queueTime)
    {
        var waitingTime = queueTime + queueTimeOffsetSec;
        yield return new WaitForSeconds(waitingTime);
        _matchmakingFallback.FallbackState = FallbackStateEnum.MatchFoundNotificationTimeout;
        if (isOnJoinSession && !onDSAvailable)
        {
            _matchmakingFallback.FallbackState = FallbackStateEnum.DSUpdateNotificationTimeout;
        }
        switch (_matchmakingFallback.FallbackState)
        {
            case FallbackStateEnum.MatchFoundNotificationTimeout:
                BytewarsLogger.Log($"UnBindMatchFoundNotification");
                UnBindMatchFoundNotification();
                StartFallback(true, false);
                break;
            case FallbackStateEnum.DSUpdateNotificationTimeout:
                BytewarsLogger.Log($"UnBindOnSessionDSUpdateNotification");
                UnBindOnSessionDSUpdateNotification();
                StartFallback(false, true);
                break;
            default:
                BytewarsLogger.Log($" is joined to session {isOnJoinSession}, is ds update {onDSAvailable}");
                break;
        }
    }

    private void StartFallback(bool isMatchNotFound, bool isServerUpdateNotFound)
    {
        switch (isMatchNotFound)
        {
            case true when !isServerUpdateNotFound:
                GetMatchmakingTicketDetails(matchTicket, true);
                break;
            case false when isServerUpdateNotFound:
                GetSessionAndDSStatus(matchTicket);
                break;
        }
    }

    private void StopFallback()
    {
        this.StopAllCoroutines();
    }

    private void PostFallback(string matchSessionId)
    {
        switch (_matchmakingFallback.FallbackState)
        {
            case FallbackStateEnum.MatchFoundNotificationTimeout:
                UnBindMatchFoundNotification();
                break;
            case FallbackStateEnum.DSUpdateNotificationTimeout:
                UnBindOnSessionDSUpdateNotification();
                _matchmakingFallback.FallbackState = FallbackStateEnum.None;
                ResetFlag();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ResetFlag()
    {
        onDSAvailable = false;
        onJoinEvent = false;
    }
    #endregion Fallback

    #region DSMatchmaking
    protected internal void StartDSMatchmaking(string matchPool)
    {

    }

    protected internal void CancelDSMatchmaking()
    {
        StopFallback();
        CancelMatchmaking(matchTicket);
    }
    #endregion DSMatchmaking

    #region EventHandler
    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {

    }

    private void OnJoiningMatch(string sessionId)
    {

    }

    private void OnCancelMatchmakingComplete()
    {
        matchTicket = null;
        sessionId = null;
        StopAllCoroutines();
    }

    private void OnPeriodicJoinSessionComplete(SessionResponsePayload response)
    {
        if (!response.IsError)
        {
            if (response.TutorialType != _tutorialType)
            {
                return;
            }
            OnMatchmakingJoinSessionCompleteEvent?.Invoke(response);
        }
        else
        {
            OnMatchmakingJoinSessionFailedEvent?.Invoke();
        }
    }

    private void GetSessionDetailsAndDSStatus(SessionResponsePayload response)
    {
        if (!response.IsError)
        {
            var session = response.Result.Value;
            GetGameSessionDetailsById(session.id);
        }
    }
    #endregion EventHandler

    #region Override
    private void GetSessionAndDSStatus(string sessionId)
    {
        GetGameSessionDetailsById(sessionId);
    }

    private void GetSessionDetails(SessionResponsePayload response)
    {
        if (!response.IsError)
        {
            if (response.TutorialType != _tutorialType) return;

            var session = response.Result.Value;
            var dsInfo = session.dsInformation;

            if (!session.isActive)
            {
                OnSessionEnded?.Invoke();
                return;
            }

            switch (dsInfo.status)
            {
                case SessionV2DsStatus.AVAILABLE:
                    BytewarsLogger.Log($"{dsInfo.status}");
                    OnDSAvailableEvent?.Invoke(session);
                    break;
                case SessionV2DsStatus.REQUESTED:
                    StartCoroutine(WaitForASecond(session.id, GetSessionAndDSStatus));
                    break;
                case SessionV2DsStatus.NEED_TO_REQUEST:
                    StartCoroutine(WaitForASecond(session.id, GetSessionAndDSStatus));
                    break;
                case SessionV2DsStatus.FAILED_TO_REQUEST:
                    OnDSFailedRequestEvent?.Invoke();
                    break;
                default:
                    BytewarsLogger.Log($"{dsInfo.status}");
                    break;
            }
        }
    }
    #endregion Override

    #region EventListener
    private void SetupMatchmakingEventListener(bool notification, bool periodically, bool fallback = false)
    {
        if (notification && periodically)
        {
            BytewarsLogger.Log($"unable to activate both");
            return;
        }

        if (notification)
        {
            BindOnMatchmakingStarted();
            BindMatchFoundNotification();
            BindMatchmakingUserJoinedGameSession();
            BindOnSessionDSUpdateNotification();

            OnMatchmakingFoundEvent += OnJoiningMatch;
            OnMatchFoundFallbackEvent += PostFallback;
        }

        if (periodically)
        {
            OnStartMatchmakingSucceedEvent += ticketId => GetMatchmakingTicketDetails(ticketId, true);
            OnMatchmakingFoundEvent += OnJoiningMatch;
            OnJoinSessionCompleteEvent += OnPeriodicJoinSessionComplete;
            OnMatchmakingJoinSessionCompleteEvent += GetSessionDetailsAndDSStatus;
            OnGetSessionDetailsCompleteEvent += GetSessionDetails;
        }

        if (fallback)
        {
            OnStartMatchmakingCompleteEvent += CheckMatchFoundNotificationFallback;
            OnUserJoinedGameSessionEvent += CheckDSStatusUpdateNotificationFallback;
            OnDSAvailableEvent += result => PostFallback(result.id);

        }
    }

    private void UnbindMatchmakingEventListener(bool notification, bool periodically, bool fallback = false)
    {
        if (notification && periodically)
        {
            BytewarsLogger.Log($"unable to activate both");
            return;
        }

        if (notification)
        {
            UnBindOnMatchmakingStarted();
            UnBindMatchFoundNotification();
            UnBindMatchmakingUserJoinedGameSession();
            UnBindOnSessionDSUpdateNotification();

            OnMatchmakingFoundEvent -= OnJoiningMatch;
        }

        if (periodically)
        {
            OnStartMatchmakingSucceedEvent -= ticketId => GetMatchmakingTicketDetails(ticketId, true);
            OnMatchmakingFoundEvent -= OnJoiningMatch;
            OnJoinSessionCompleteEvent -= OnPeriodicJoinSessionComplete;
            OnMatchmakingJoinSessionCompleteEvent -= GetSessionDetailsAndDSStatus;
            OnGetSessionDetailsCompleteEvent -= GetSessionDetails;
        }
    }

    private void BindOnSessionDSUpdateNotification()
    {
        Lobby.SessionV2DsStatusChanged += OnSessionDSUpdate;
    }

    private void UnBindOnSessionDSUpdateNotification()
    {
        Lobby.SessionV2DsStatusChanged -= OnSessionDSUpdate;
    }

    private void OnSessionDSUpdate(Result<SessionV2DsStatusUpdatedNotification> result)
    {

    }
    #endregion EventListener

    #region GameServer
#if UNITY_SERVER
    #region GameServerNotification
    private void MatchMakingServerClaim()
    {

    }

    private void BackFillProposal()
    {

    }

    private void OnBackfillProposalReceived(MatchmakingV2BackfillProposalNotification proposal, bool isStopBackfilling)
    {

    }

    private void OnBackfillProposalRejected(MatchmakingV2BackfillProposalNotification proposal)
    {

    }
    #endregion GameServerNotification
#endif
    #endregion GameServer
}
