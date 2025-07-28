// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using static MatchSessionEssentialsModels;

public class MatchSessionEssentialsWrapper : SessionEssentialsWrapper
{
    public static ResultCallback<SessionV2DsStatusUpdatedNotification> OnDSStatusChanged = delegate { };

    public virtual void BrowseGameSessions(
        string pageUrl,
        ResultCallback<BrowseSessionResult> onComplete)
    {
        onComplete?.Invoke(Result<BrowseSessionResult>.CreateError(ErrorCode.NotImplemented));
    }
}
