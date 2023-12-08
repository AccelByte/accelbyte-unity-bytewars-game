using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using Unity.Netcode;

public class GameSessionEssentialsWrapper : SessionEssentialsWrapper
{
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
        _dedicatedServerManager = MultiRegistry.GetServerApiClient().GetDedicatedServerManager();
        DedicatedServerManager = _dedicatedServerManager;
#endif
    }

    #region GameClient

    protected internal void TravelToDS(SessionV2GameSession session)
    {
        var dsInfo = session.dsInformation;
        int port = ConnectionHandler.DefaultPort;
        
        if (dsInfo.server.ports.Count > 0)
        {
            dsInfo.server.ports.TryGetValue("unityds", out port);
        }
        else
        {
            port = dsInfo.server.port;
        }

        var initialData = new InitialConnectionData()
        {
            sessionId = "",
            inGameMode = InGameMode.None,
            serverSessionId = session.id
        };

        switch (session.matchPool)
        {
            case "unity-elimination-ds":
                initialData.inGameMode = InGameMode.OnlineEliminationGameMode;
                GameManager.Instance.StartAsClient(dsInfo.server.ip, (ushort)port, initialData);
                break;
            case "unity-teamdeathmatch-ds":
                initialData.inGameMode = InGameMode.OnlineDeathMatchGameMode;
                GameManager.Instance.StartAsClient(dsInfo.server.ip, (ushort)port, initialData);
                break;
        }
    } 

    //overload TravelToDS
    protected internal void TravelToDS(SessionV2GameSession sessionV2Game, InGameMode gameMode)
    {
        if (NetworkManager.Singleton.IsListening) return;
        int port = ConnectionHandler.DefaultPort;
        if (sessionV2Game.dsInformation.server.ports.Count > 0)
        {
            sessionV2Game.dsInformation.server.ports.TryGetValue("unityds", out port);
        }
        else
        {
            port = sessionV2Game.dsInformation.server.port;
        }
        var ip = sessionV2Game.dsInformation.server.ip;
        var portUshort = (ushort)port;
        var initialData = new InitialConnectionData()
        {
            sessionId = "",
            inGameMode = gameMode,
            serverSessionId = sessionV2Game.id
        };
        GameManager.Instance
            .StartAsClient(ip, portUshort, initialData);
    }

    #endregion

}
