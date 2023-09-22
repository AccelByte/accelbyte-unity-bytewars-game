// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;

/// <summary>
///  A class wrapper to return after session 
/// </summary>
public class SessionResponsePayload
{
    public Result<SessionV2GameSession> Result { get; set; }
    public bool IsError { get => Result is { IsError: true }; set => IsError = value; }
    public TutorialType? TutorialType { get; set; }

}