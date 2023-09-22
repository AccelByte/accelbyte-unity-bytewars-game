using System;
using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class AuthEssentialsWrapper : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private ApiClient apiClient;
    private User user;
    
    // required variables to login with other platform outside AccelByte
    private PlatformType _platformType;
    private string _platformToken;

    public TokenData userData;

    void Start()
    {
        apiClient = MultiRegistry.GetApiClient();
        user = apiClient.GetApi<User, UserApi>();
    }

    #region AB Service Functions

    public void Login(LoginType loginMethod, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        switch (loginMethod)
        {
            case LoginType.DeviceId:
                _platformType = PlatformType.Device;
                _platformToken = SystemInfo.deviceUniqueIdentifier;
                break;
        }
        
        Debug.Log("[AuthEssentials] Trying to login with device id: "+ SystemInfo.deviceUniqueIdentifier);
        
        user.LoginWithOtherPlatform(_platformType, _platformToken, result => OnLoginCompleted(result, resultCallback));
    }
    
    public void LoginWithUsername(string username, string password, ResultCallback<TokenData, OAuthError> resultCallback)
    {
        user.LoginWithUsernameV3(username, password, result => OnLoginCompleted(result, resultCallback), false);
    }
    
    private void GetUserPublicData(string receivedUserId)
    {
        GameData.CachedPlayerState.playerId = receivedUserId;
        user.GetUserByUserId(receivedUserId, OnGetUserPublicDataFinished);
    }

    /// <summary>
    /// Get user info of some users in bulk
    /// </summary>
    /// <param name="userIds">an array of user id from the desired users</param>
    /// <param name="resultCallback">callback function to get result from other script</param>
    public void BulkGetUserInfo(string[] userIds, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        user.BulkGetUserInfo(userIds, result => OnBulkGetUserInfoCompleted(result, resultCallback));
    }
    
    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        user.GetUserAvatar(userId, result => OnGetUserAvatarCompleted(result, resultCallback));
    }

    #endregion
    
    #region Callback Functions

    /// <summary>
    /// Default Callback for LoginWithOtherPlatform() function
    /// </summary>
    /// <param name="result">result of the LoginWithOtherPlatform() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnLoginCompleted(Result<TokenData, OAuthError> result, ResultCallback<TokenData, OAuthError> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Login user successful.");
            GameData.CachedPlayerState.playerId = result.Value.user_id;
            userData = result.Value;
        }
        else
        {
            Debug.Log($"Login user failed. Message: {result.Error.error}");
        }

        customCallback?.Invoke(result);
    }
    
    private void OnGetUserPublicDataFinished(Result<PublicUserData> result)
    {
        if (result.IsError)
        {
            Debug.Log($"[AuthEssentialsWrapper] error OnGetUserPublicDataFinished:{result.Error.Message}");
        }
        else
        {
            var publicUserData = result.Value;
            GameData.CachedPlayerState.playerId = publicUserData.userId;
            GameData.CachedPlayerState.avatarUrl = publicUserData.avatarUrl;
            GameData.CachedPlayerState.playerName = publicUserData.displayName;
        }
    }

    /// <summary>
    /// Default Callback for BulkGetUserInfo() function
    /// </summary>
    /// <param name="result">result of the BulkGetUserInfo() function call</param>
    /// <param name="customCallback">additional callback function that can be customized from other script</param>
    private void OnBulkGetUserInfoCompleted(Result<ListBulkUserInfoResponse> result, ResultCallback<ListBulkUserInfoResponse> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log("Successfully bulk get the user info!");
        }
        else
        {
            Debug.Log($"Unable to bulk get the user info. Message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    private void OnGetUserAvatarCompleted(Result<Texture2D> result, ResultCallback<Texture2D> customCallback = null)
    {
        if (!result.IsError)
        {
            Debug.Log($"Successfully retrieve the user avatar! ");
        }
        else
        {
            Debug.LogWarning($"Unable to retrieve the user avatar. Message: {result.Error}");
        }
        
        customCallback?.Invoke(result);
    }

    #endregion
}
