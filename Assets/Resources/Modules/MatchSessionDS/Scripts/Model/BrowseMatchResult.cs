// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;

public struct BrowseMatchResult
{
    public BrowseMatchResult(SessionV2GameSession[] result, string errorMessage="")
    {
        Result = result;
        ErrorMessage = errorMessage;
    }
    public readonly SessionV2GameSession[] Result;
    public readonly string ErrorMessage;
}