using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class ManagingFriendsWrapper : MonoBehaviour
{
    private Lobby _lobby;

    // Start is called before the first frame update
    void Start()
    {
        _lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
    }

    public void Unfriend(string userId, ResultCallback resultCallback)
    {
        _lobby.Unfriend(userId, result =>
        {
            if (!result.IsError) {
                Debug.Log($"Success to unfriend");
            } else {
                Debug.LogWarning($"Error unfriend a friend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        } );
    }
    
    public void BlockPlayer(string userId, ResultCallback<BlockPlayerResponse> resultCallback)
    {
        _lobby.BlockPlayer(userId, result =>
        {
            if (!result.IsError) {
                Debug.Log($"Success to block a player");
            } else {
                Debug.LogWarning($"Error unfriend a friend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        } );
    }
    
    public void UnblockPlayer(string userId, ResultCallback<UnblockPlayerResponse> resultCallback)
    {
        _lobby.UnblockPlayer(userId, result =>
        {
            if (!result.IsError) {
                Debug.Log($"Success to unblock a player");
            } else {
                Debug.LogWarning($"Error unblock a friend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        } );
    }

    public void GetBlockedPlayers(ResultCallback<BlockedList> resultCallback)
    {
        _lobby.GetListOfBlockedUser(result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Success to load blocked users, total blocked users {result.Value.data.Length}");
            }
            else
            {
                Debug.LogWarning($"Error to load blocked users, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        });
    }
}
