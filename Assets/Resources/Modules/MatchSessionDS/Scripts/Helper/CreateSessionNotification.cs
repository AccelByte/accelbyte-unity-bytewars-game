using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public static class CreateSessionNotification
{
    private const string ClassName = "[CreateSessionNotification]";
    public static void OnV2GameSessionUpdated(Result<SessionV2GameSessionUpdatedNotification> result)
    {
        LogJson("session updated", result);
    }

    private static void LogJson<T>(string prefix, Result<T> result)
    {
        if (result.IsError)
        {
            Debug.Log($"{ClassName} fail {prefix} {result.Error.Message}");
        }
        else
        {
            Debug.Log($"{ClassName} success {prefix} {result.Value.ToJsonString()}");
        }
    }

    public static void OnV2GameSessionMemberChanged(Result<SessionV2GameMembersChangedNotification> result)
    {
        LogJson("member changed", result);
    }

    public static void OnV2UserJoinedGameSession(Result<SessionV2GameJoinedNotification> result)
    {
        LogJson("user joined game session", result);
    }

    public static void OnV2UserKickedFromGameSession(Result<SessionV2GameUserKickedNotification> result)
    {
        LogJson("user kicked from game session", result);
    }

    public static void OnV2UserRejectedGameSessionInvitaion(Result<SessionV2GameInvitationRejectedNotification> result)
    {
        LogJson("user rejected game session invitation", result);
    }

    public static void OnV2InvitedUserToGameSession(Result<SessionV2GameInvitationNotification> result)
    {
        LogJson("invited user to game session", result);
    }

    public static void OnV2DsStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        LogJson("Ds status changed", result);
    }
}
