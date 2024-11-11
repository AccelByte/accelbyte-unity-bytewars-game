// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameSessionUtilityWrapper : SessionEssentialsWrapper
{
    private const string EliminationDSMatchPool = "unity-elimination-ds";
    private const string EliminationDSAMSMatchPool = "unity-elimination-ds-ams";
    private const string TeamDeathmatchDSMatchPool = "unity-teamdeathmatch-ds";
    private const string TeamDeathmatchDSAMSMatchPool = "unity-teamdeathmatch-ds-ams";

#if UNITY_SERVER
    private DedicatedServerManager _dedicatedServerManager;
    
    public DedicatedServerManager DedicatedServerManager 
    { 
        get => _dedicatedServerManager;
        private set => _dedicatedServerManager = value; 
    }
#endif

    protected void Awake()
    {
        base.Awake();
#if UNITY_SERVER
        _dedicatedServerManager = AccelByteSDK.GetServerRegistry().GetApi().GetDedicatedServerManager();
        DedicatedServerManager = _dedicatedServerManager;
#endif
    }

    #region GameClient
    protected internal void TravelToDS(SessionV2GameSession session)
    {
        SessionV2DsInformation dsInfo = session.dsInformation;
        ushort port = GetPort(session.dsInformation);

        InitialConnectionData initialData = new InitialConnectionData()
        {
            sessionId = "",
            inGameMode = InGameMode.None,
            serverSessionId = session.id
        };

        switch (session.matchPool)
        {
            case EliminationDSMatchPool:
            case EliminationDSAMSMatchPool:
                initialData.inGameMode = InGameMode.OnlineEliminationGameMode;
                GameManager.Instance.StartAsClient(dsInfo.server.ip, port, initialData);
                break;
            case TeamDeathmatchDSMatchPool:
            case TeamDeathmatchDSAMSMatchPool:
                initialData.inGameMode = InGameMode.OnlineDeathMatchGameMode;
                GameManager.Instance.StartAsClient(dsInfo.server.ip, port, initialData);
                break;
        }
    }

    //overload TravelToDS
    protected internal void TravelToDS(SessionV2GameSession sessionV2Game, InGameMode gameMode)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            return;
        }

        StartCoroutine(ShowTravelingLoadingCoroutine(() => StartClient(sessionV2Game, gameMode)));
    }

    public IEnumerator ShowTravelingLoadingCoroutine(Action action)
    {
        MenuManager.Instance.ShowLoading("Traveling");
        yield return new WaitForSeconds(1);
        action?.Invoke();
    }

    private void StartClient(SessionV2GameSession sessionV2Game, InGameMode gameMode)
    {

        ushort port = GetPort(sessionV2Game.dsInformation);
        string ip = sessionV2Game.dsInformation.server.ip;
        InitialConnectionData initialData = new InitialConnectionData()
        {
            sessionId = "",
            inGameMode = gameMode,
            serverSessionId = sessionV2Game.id
        };
        GameManager.Instance
            .StartAsClient(ip, port, initialData);
    }

    private ushort GetPort(SessionV2DsInformation dsInformation)
    {
        int port = ConnectionHandler.DefaultPort;
        if (dsInformation.server.ports.Count > 0)
        {
            dsInformation.server.ports.TryGetValue("default_port", out port);
        }
        if (port == 0)
        {
            port = dsInformation.server.port;
        }
        return (ushort)port;
    }
    #endregion GameClient

}
