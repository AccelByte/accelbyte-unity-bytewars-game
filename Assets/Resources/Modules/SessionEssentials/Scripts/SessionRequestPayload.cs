// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Models;
using UnityEngine;

public class SessionRequestPayload
{
    public SessionType SessionType { get; set; }
    public string SessionId { get; set; }
    public string MatchPool { get; set; }
    public InGameMode? InGameMode { get; set; }
    public TutorialType? TutorialType { get; set; }
    public Dictionary<string, object>  attributes { get; set; }

    public void Clear()
    {
        SessionType = SessionType.none;
        SessionId = null;
        MatchPool = null;
        InGameMode = null;
        TutorialType = null;
        attributes = null;
    }
}