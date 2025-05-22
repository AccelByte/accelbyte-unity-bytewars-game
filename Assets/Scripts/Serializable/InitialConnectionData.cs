// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;

[Serializable]
public class InitialConnectionData
{
    public InGameMode inGameMode;
    
    // Unity NetCode GameObject connection session ID, for reconnection purpose.
    public string sessionId = string.Empty;
    
    // AccelByte session ID.
    public string serverSessionId = string.Empty;

    // AccelByte user ID.
    public string userId = string.Empty;
}
