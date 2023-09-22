using System;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class FriendEssentialsWrapper : MonoBehaviour
{
    private User _user;
    private Lobby _lobby;

    public string PlayerUserId { get; private set; }
    private static Friends _friends;
    private static Friends _outgoingFriends;

    public static event Action OnRejected;
    public static event Action OnIncomingAdded;
    public static event Action OnAccepted;
    
    // Start is called before the first frame update
    void Start()
    {
        _user = MultiRegistry.GetApiClient().GetUser();
        _lobby = MultiRegistry.GetApiClient().GetLobby();
        LoginHandler.onLoginCompleted += tokenData =>
        {
            PlayerUserId = tokenData.user_id;
        };
        LoginHandler.onLoginCompleted += tokenData => LoginToLobby();
        LoginHandler.onLoginCompleted += tokenData => ListenIncomingFriendRequest();
        LoginHandler.onLoginCompleted += tokenData => ListenRejectedRequest();
        LoginHandler.onLoginCompleted += tokenData => ListenAcceptedRequest();
    }

    private void LoginToLobby()
    {
        if (!_lobby.IsConnected)
        {
            _lobby.Connect();
        }
    }
    
    public void LoadIncomingFriendRequests(ResultCallback<Friends> resultCallback)
    {
        _lobby.ListIncomingFriends(result =>
        {
            if (!result.IsError) {
                Debug.Log($"Succes to load incoming friend request");
            } else {
                Debug.LogWarning($"Error ListIncomingFriends, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        });
    }
    
    public void DeclineFriend(string userID, ResultCallback resultCallback)
    {
        _lobby.RejectFriend(userID, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"success to reject friend request {userID}");
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
            
            resultCallback?.Invoke(result);

        });
    }

    public void AcceptFriend(string userID, ResultCallback resultCallback)
    {
        _lobby.AcceptFriend(userID, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Success to accept friend {userID}");
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
            
            resultCallback?.Invoke(result);
        });
    }
    
    public void CancelFriendRequests(string userID, ResultCallback resultCallback)
    {
        _lobby.CancelFriendRequest(userID, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Success to cancel outgoing friend request {userID}");
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
            
            resultCallback?.Invoke(result);
        });
    }

    #region Module-8a

    public void GetUserByDisplayName(string displayName, ResultCallback<PagedPublicUsersInfo> resultCallback)
    {
        var searcRules = SearchType.DISPLAYNAME;
        _user.SearchUsers(displayName, searcRules, result =>
        {
            if (!result.IsError) {
                Debug.Log($"Success to search users with displayname {displayName}");
                resultCallback?.Invoke(result);
            } 
            else
            {
                Debug.LogWarning($"Error SearchUsers, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
        });
    }

    public void LoadOutgoingFriendRequests(ResultCallback<Friends> resultCallback = null)
    {
        _lobby.ListOutgoingFriends(result => {
            if (!result.IsError) {
                //Save outgoing value to _outgoingFriends
                _outgoingFriends = result.Value;
            } else {
                Debug.LogWarning($"Error ListOutgoingFriends, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        });
    }

    public void GetFriendshipStatus(string userId, ResultCallback<FriendshipStatus> resultCallback)
    {
        _lobby.GetFriendshipStatus(userId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"success to get friendship status for {userId}");
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
            
            resultCallback.Invoke(result);
        });
    }
    
    public void GetFriendList(ResultCallback<Friends> resultCallback = null)
    {
        _lobby.LoadFriendsList(result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Success to get friend list");
            }
            else
            {
                Debug.LogWarning(
                    $"Error LoadFriendsList, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
            resultCallback?.Invoke(result);
        });
    }
    

    public void ListenIncomingFriendRequest()
    {
        _lobby.OnIncomingFriendRequest += result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Successfully listened for incoming friend request");
                OnIncomingAdded?.Invoke();
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
        };
    }
    
    public void ListenRejectedRequest()
    {
        _lobby.FriendRequestRejected += result =>
        {
            if (!result.IsError) {
                Debug.Log($"Successfully listened for outgoing friend request rejection");
                OnRejected?.Invoke();
            } else {
                Debug.LogWarning($"Error OnUnfriend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
        };
    }
    
    public void ListenAcceptedRequest()
    {
        _lobby.FriendRequestAccepted += result =>
        {
            if (!result.IsError) {
                Debug.Log($"Successfully listened for outgoing friend request acceptance");
                OnAccepted?.Invoke();
            } else {
                Debug.LogWarning($"Error OnUnfriend, Error Code: {result.Error.Code} Error Message: {result.Error.Message}");
            }
        };
    }

    public void GetBulkUserInfo(string[] usersId, ResultCallback<ListBulkUserInfoResponse> resultCallback)
    {
        _user.BulkGetUserInfo(usersId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"success to retrieve bulk user info {!result.IsError}");
                resultCallback?.Invoke(result);
            }
            else
            {
                Debug.LogWarning($"{result.Error.Message}");
            }
        });
    }
    
    public void GetUserAvatar(string userId, ResultCallback<Texture2D> resultCallback)
    {
        _user.GetUserAvatar(userId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"Success to retrieve Avatar for User {userId}");
            }
            else
            {
                Debug.LogWarning($"Unable to retrieve Avatar for User {userId} : {result.Error}");
            }
            resultCallback?.Invoke(result);
        });
    }

    public void SendFriendRequest(string userId, ResultCallback resultCallback)
    {
        _lobby.RequestFriend(userId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log("Sent Friends Request");
                resultCallback?.Invoke(result);
            }
            else
            {
                Debug.LogWarning($"Failed to send a friends request: error code: {result.Error.Code} message: {result.Error.Message}");
            }
        });
    }

    #endregion
    
    #region Utilities
    
    public bool IsAlreadyFriend(string userId)
    {
        var isFriend = _friends.friendsId.Any(x => x == userId);
        return isFriend;
    }
    
    public bool CheckOutGoingFriendRequest(string userId)
    {
        var isInOutgoingList = _outgoingFriends.friendsId.Any(x => x == userId);

        if (isInOutgoingList)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void GetUserData(string userId)
    {
        _user.GetUserByUserId(userId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"{userId}, {result.Value.avatarUrl}");
            }
            else
            {
                Debug.LogWarning($"{result.IsError}, {result.Error.Message}");
            }
        });
    }
    
    public void GetOwnData(string userId)
    {
        _user.GetUserByUserId(userId, result =>
        {
            if (!result.IsError)
            {
                Debug.Log($"{userId}, {result.Value.avatarUrl}");
            }
            else
            {
                Debug.LogWarning($"{result.IsError}, {result.Error.Message}");
            }
        });
    }
    
    #endregion
}
