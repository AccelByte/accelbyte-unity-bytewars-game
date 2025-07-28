// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;

public class MatchmakingEssentialsWrapper : SessionEssentialsWrapper
{
    protected static MatchmakingV2 Matchmaking;

#if UNITY_SERVER
    protected static ServerDSHub ServerDSHub;
    protected static ServerMatchmakingV2 ServerMatchmaking;
#endif

    public static ResultCallback<MatchmakingV2CreateTicketResponse> OnMatchmakingStarted = delegate { };
    public static ResultCallback<MatchmakingV2MatchFoundNotification> OnMatchFound = delegate { };
    public static ResultCallback OnMatchmakingCanceled = delegate { };
    public static ResultCallback<MatchmakingV2TicketExpiredNotification> OnMatchmakingExpired = delegate { };
    public static ResultCallback<SessionV2GameInvitationNotification> OnSessionInviteReceived = delegate { };
    public static ResultCallback<SessionV2DsStatusUpdatedNotification> OnDSStatusChanged = delegate { };

    protected override void Awake()
    {
        base.Awake();

        Matchmaking ??= AccelByteSDK.GetClientRegistry().GetApi().GetMatchmakingV2();

#if UNITY_SERVER
        ServerDSHub ??= AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
        ServerMatchmaking ??= AccelByteSDK.GetServerRegistry().GetApi().GetMatchmakingV2();
#endif
    }

    public virtual void StartMatchmaking(
        string matchPool,
        ResultCallback<MatchmakingV2CreateTicketResponse> onComplete)
    {
        onComplete?.Invoke(Result<MatchmakingV2CreateTicketResponse>.CreateError(ErrorCode.NotImplemented));
    }

    public virtual void CancelMatchmaking(
        string matchTicketId,
        ResultCallback onComplete)
    {
        onComplete?.Invoke(Result.CreateError(ErrorCode.NotImplemented));
    }
}