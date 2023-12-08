// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.Collections;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using UnityEngine;
using static MatchmakingFallback;

public class MatchmakingSessionDSWrapper : MatchmakingSessionWrapper
{
    private ServerMatchmakingV2 _matchmakingV2Server;
    private ServerDSHub _serverDSHub;
    private MatchmakingFallback _matchmakingFallback = new MatchmakingFallback();
    
    private string _matchTicket;
    private bool _isGameStarted;
    private string _sessionId;
    private bool _isSessionActive;
    private bool _isOnJoinSession;
    private bool _isDSUpdateError;
    private bool _onJoinEvent;
    private bool _onDSAvailable;
    private bool _isFired = false;

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
        
        _serverDSHub = MultiRegistry.GetServerApiClient().GetDsHub();
        _matchmakingV2Server = MultiRegistry.GetServerApiClient().GetMatchmakingV2();
    }
    
    private void Start()
    {
#if UNITY_SERVER
        var dsEssentialModule = TutorialModuleManager.Instance.GetModule(TutorialType.MultiplayerDSEssentials);
        if (dsEssentialModule.isStarterActive)
        {
            var multiplayerDSEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSEssentialsWrapper_Starter>();
            multiplayerDSEssentialsWrapper.OnLoginServerCompleteEvent += MatchMakingServerClaim;
            multiplayerDSEssentialsWrapper.OnLoginServerCompleteEvent += BackFillProposal;
        }
        else
        {
            var multiplayerDSEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<MultiplayerDSEssentialsWrapper>();
            multiplayerDSEssentialsWrapper.OnLoginServerCompleteEvent += MatchMakingServerClaim;
            multiplayerDSEssentialsWrapper.OnLoginServerCompleteEvent += BackFillProposal;
        }
        GameManager.Instance.OnRejectBackfill += () => { _isGameStarted = true; };
        GameManager.Instance.OnGameStateIsNone += () => { _isGameStarted = false; };
#endif

        //listen to event
        QuickPlayMenuHandler.OnMenuEnable += BindEventListener;
        QuickPlayMenuHandler.OnMenuDisable += UnbindEventListener;
    }

    private void BindEventListener()
    {
        SetupMatchmakingEventListener(true, false);
        OnStartMatchmakingCompleteEvent += OnStartMatchmakingComplete;
        OnCancelMatchmakingCompleteEvent += OnCancelMatchmakingComplete;
    }

    private void UnbindEventListener()
    {
        UnbindMatchmakingEventListener(true, false);
        OnStartMatchmakingCompleteEvent -= OnStartMatchmakingComplete;
        OnCancelMatchmakingCompleteEvent -= OnCancelMatchmakingComplete;
        _isFired = false;
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
        var waitingTime = queueTime + 5;
        yield return new WaitForSeconds(waitingTime);
        _matchmakingFallback.FallbackState = FallbackStateEnum.MatchFoundNotificationTimeout;
        if (_isOnJoinSession == true && _onDSAvailable == false)
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
                BytewarsLogger.Log( $" is joined to session {_isOnJoinSession}, is ds update {_onDSAvailable}");
                break;
        }
    }
    
    private void StartFallback(bool isMatchNotFound, bool isServerUpdateNotFound)
    {
        switch (isMatchNotFound)
        {
            case true when !isServerUpdateNotFound:
                GetMatchmakingTicketDetails(_matchTicket, true);
                break;
            case false when isServerUpdateNotFound:
                GetSessionAndDSStatus(_matchTicket);
                break;
        }
    }
    
    private void StopFallback()
    {
        this.StopAllCoroutines();
    }

    private void PostFallback()
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
        _onDSAvailable = false;
        _onJoinEvent = false;
    }
    
    #endregion

    #region DSMatchmaking

    protected internal void StartDSMatchmaking(string matchPool)
    {
        var isLocal = ConnectionHandler.GetArgument();
        StartMatchmaking(matchPool, isLocal);
    }

    protected internal void CancelDSMatchmaking()
    {
        StopFallback();
        CancelMatchmaking(_matchTicket);
    }
    
    #endregion
    
    #region EventHandler

    private void OnStartMatchmakingComplete(Result<MatchmakingV2CreateTicketResponse> result)
    {
        if (!result.IsError)
        {
            _matchTicket = result.Value.matchTicketId;
            OnStartMatchmakingSucceedEvent?.Invoke(_matchTicket);
        }
        else
        {
            OnStartMatchmakingFailed?.Invoke();
        }
    }
    
    private void OnJoiningMatch(string sessionId)
    {
        _isOnJoinSession = true;
        JoinSession(sessionId);
    }

    private void OnCancelMatchmakingComplete()
    {
        _matchTicket = null;
        _sessionId = null;
        StopAllCoroutines();
    }
    
    private void OnPeriodicJoinSessionComplete(SessionResponsePayload response)
    {
        BytewarsLogger.Log("test raised event");
        if (!response.IsError)
        {
            if (response.TutorialType != _tutorialType) return;
            OnMatchmakingJoinSessionCompleteEvent?.Invoke(response);
        }
        else
        {
            OnMatchmakingJoinSessionFailedEvent?.Invoke();
        }
    }
    
    private void GetSessionDetailsAndDSStatus(SessionResponsePayload response)
    {
        BytewarsLogger.Log("test raised event");
        if (!response.IsError)
        {
            var session = response.Result.Value;
            GetGameSessionDetailsById(session.id);
        }
    }

    #endregion
    
    #region Override
    
    private void GetSessionAndDSStatus(string sessionId)
    {
        GetGameSessionDetailsById(sessionId);
    }
    
    private void GetSessionDetails(SessionResponsePayload response)
    {
        BytewarsLogger.Log($"test");
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

    #endregion
    
    #region EventListener

    private void SetupMatchmakingEventListener(bool notification, bool periodically, bool fallback = false)
    {
        if (notification && periodically == true)
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
            OnDSAvailableEvent += result => PostFallback();

        }
    }
    

    private void UnbindMatchmakingEventListener(bool notification, bool periodically, bool fallback = false)
    {
        if (notification && periodically == true)
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
        _lobby.SessionV2DsStatusChanged += OnSessionDSUpdate;
    }

    private void UnBindOnSessionDSUpdateNotification()
    {
        _lobby.SessionV2DsStatusChanged -= OnSessionDSUpdate;
    }

    private void OnSessionDSUpdate(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (!result.IsError)
        {
            var session = result.Value.session;
            var dsInfo = session.dsInformation;
            switch (dsInfo.status)
            {
                case SessionV2DsStatus.AVAILABLE:
                    if (!_isFired)
                    {
                        _isFired = true;
                        OnDSAvailableEvent?.Invoke(session);
                    }
                    break;
                case SessionV2DsStatus.FAILED_TO_REQUEST:
                    BytewarsLogger.LogWarning($"{dsInfo.status}");
                    OnDSFailedRequestEvent?.Invoke();
                    break;
                default:
                    BytewarsLogger.Log($"{dsInfo.status}");
                    break;
            }
        }
        else
        {
            Debug.Log($"{result.Error.Message}");
        } 
    }

    #endregion

    #region GameServer
    
#if UNITY_SERVER
    #region GameServerNotification

    private void MatchMakingServerClaim()
    {
        _serverDSHub.MatchmakingV2ServerClaimed += result =>
        {
            if (!result.IsError)
            {
                var serverSession = result.Value.sessionId;
                GameData.ServerSessionID = serverSession;
                BytewarsLogger.Log($"Server Claimed and Assigned to sessionId = {serverSession}");
            }
            else
            {
                BytewarsLogger.LogWarning($"Failed to get server claim event from server");
            }
        };
    }

    private void BackFillProposal()
    {
        _serverDSHub.MatchmakingV2BackfillProposalReceived += result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"BackFillProposal");

                if (!_isGameStarted)
                {
                    OnBackfillProposalReceived(result.Value, _isGameStarted );
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
        _matchmakingV2Server.AcceptBackfillProposal(proposal, isStopBackfilling, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Back-filling accepted {!isStopBackfilling}");
            }
        });
    }
    
    private void OnBackfillProposalRejected(MatchmakingV2BackfillProposalNotification proposal)
    {
        _matchmakingV2Server.RejectBackfillProposal(proposal, true, result =>
        {
            if (!result.IsError)
            {
                BytewarsLogger.Log($"Back-filling rejected - Game already started");
            }
        });
    }

    #endregion
#endif

    #endregion
}
