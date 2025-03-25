// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PresenceEssentialsWrapper_Starter : MonoBehaviour
{
    private static ApiClient ApiClient => AccelByteSDK.GetClientRegistry().GetApi();
    private Lobby lobby;

    private string SceneStatus { get; set; } = string.Empty;
    private string GameModeStatus { get; set; } = string.Empty;
    private string CachedActivity { get; set; } = string.Empty;
    private bool IsInParty { get; set; } = false;

    private readonly List<UserStatusNotif> cachedUserPresence = new();
    
    public static event Action<FriendsStatusNotif> OnFriendsStatusChanged = delegate { };
    public static event Action<BulkUserStatusNotif> OnBulkUserStatusReceived = delegate { };
    
    private void Awake()
    {
        lobby = ApiClient.GetLobby();

        LoginHandler.OnLoginComplete += CheckLobbyConnection;
    }
    
    private void OnDestroy()
    {
        LoginHandler.OnLoginComplete -= CheckLobbyConnection;
    }
    
    private void CheckLobbyConnection(TokenData _)
    {
        if (!lobby.IsConnected)
        {
            lobby.Connect();
        }
    }

    #region User Presence Module

    // TODO: Implement User Presence Module functions here.

    #endregion
}
