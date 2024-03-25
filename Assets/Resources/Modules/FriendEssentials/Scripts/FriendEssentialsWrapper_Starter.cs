using System;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class FriendEssentialsWrapper_Starter : MonoBehaviour
{
    #region Predefined-8a

    private User _user;
    private Lobby _lobby;
    public string PlayerUserId { get; private set; }
    
    #endregion

    #region Predefined-8b
    
    public static event Action OnRejected;
    public static event Action OnIncomingAdded;
    public static event Action OnAccepted;
    
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // Predefined code
        _user = AccelByteSDK.GetClientRegistry().GetApi().GetUser();
        _lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
        LoginHandler.onLoginCompleted += tokenData =>
        {
            PlayerUserId = tokenData.user_id;
        };
        LoginHandler.onLoginCompleted += tokenData => LoginToLobby();

    }

    /// <summary>
    /// Predefined code
    /// </summary>
    private void LoginToLobby()
    {
        if (!_lobby.IsConnected)
        {
            _lobby.Connect();
        }
    }

}
