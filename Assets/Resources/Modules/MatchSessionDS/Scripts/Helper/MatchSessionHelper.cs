using System.IO;
using System.Runtime.CompilerServices;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using Unity.Netcode;
using UnityEngine;

public static class MatchSessionHelper
{
    private static ApiClient _apiClient;
    private static User _user;
    private static void Init()
    {
        if (_user==null)
        {
            _apiClient = MultiRegistry.GetApiClient();
            _user = _apiClient.GetApi<User, UserApi>();
        }
    }
    
    #region GetCurrentUserPublicData
    public static void GetCurrentUserPublicData(string receivedUserId)
    {
        Init();
        GameData.CachedPlayerState.playerId = receivedUserId;
        _user.GetUserByUserId(receivedUserId, OnGetUserPublicDataFinished);
    }

    private static void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            BytewarsLogger.LogWarning($"error OnGetUserPublicDataFinished:{result.Error.Message}");
        }
        else
        {
            var publicUserData = result.Value;
            GameData.CachedPlayerState.playerId = publicUserData.userId;
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = publicUserData.displayName;
        }
    }
    #endregion GetCurrentUserPublicData

    #region LogResult

    public static void LogResult<T>(Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!result.IsError)
        {
            Debug.Log($"[{Path.GetFileName(sourceFilePath)}] [{memberName}] [Log] [{sourceLineNumber}] - {result.Value.ToJsonString()}");
        }

        {
            Debug.LogWarning($"[{Path.GetFileName(sourceFilePath)}] [{memberName}] [Warning] [{sourceLineNumber}] - {result.Error.Message}");
        }
    }

    #endregion


}
