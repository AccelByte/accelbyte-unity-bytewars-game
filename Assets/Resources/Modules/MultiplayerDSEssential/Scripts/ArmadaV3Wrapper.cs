// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.
#if UNITY_SERVER

using System;
using AccelByte.Core;
using AccelByte.Server;

public class ArmadaV3Wrapper : GameSessionEssentialsWrapper, IDSService
{
    public event Action OnInstantiateComplete;
    public event ResultCallback OnLoginCompleteEvent;
    public event ResultCallback OnRegisterCompleteEvent;
    public event ResultCallback OnUnregisterCompleteEvent;
    
    private ServerDSHub _serverDSHub;


    private new void Awake()
    {
        base.Awake();
#if UNITY_SERVER
        _serverDSHub = MultiRegistry.GetServerApiClient().GetDsHub();
#endif
    }

    private void Start()
    {
        OnInstantiateComplete?.Invoke();
    }
    
    public void LoginServer()
    {
        AccelByteServerPlugin.GetDedicatedServer().LoginWithClientCredentials(result =>
        {
            BytewarsLogger.Log(!result.IsError
                ? "Server login successful"
                : $"Server login failed : {result.Error.Code}: {result.Error.Message}");
            OnLoginCompleteEvent?.Invoke(result);
        });
    }
    
    public void RegisterServer()
    {
        var isLocalServer = ConnectionHandler.GetArgument();
        if (!isLocalServer)
        {
            // Register Cloud Server to DSM (ARMADA)
            DedicatedServerManager.RegisterServer((int)ConnectionHandler.DefaultPort, result => OnRegisterServerComplete(result, false));
        }
        else
        {
            var ip = ConnectionHandler.LocalServerIP;
            var serverName = ConnectionHandler.LocalServerName;
            var portNumber = Convert.ToUInt32(ConnectionHandler.DefaultPort);
            
            // Register Local Server to DSM (ARMADA)
            DedicatedServerManager.RegisterLocalServer(ip, portNumber, serverName, result =>  OnRegisterServerComplete(result, true));
        }
    }
    
    public void ConnectToDSHub()
    {
        _serverDSHub.OnConnected += () =>
        {
            BytewarsLogger.Log($"Success to log in {DedicatedServerManager.ServerName} to DS Hub");
        };
        
        _serverDSHub.Connect(DedicatedServerManager.ServerName);
    }

    public void ListenOnDisconnect()
    {
        _serverDSHub.OnDisconnected += (WsCloseCode code)=>
        {
            BytewarsLogger.Log("disconnected from server ds hub, try to reconnect");
            if(!String.IsNullOrEmpty(DedicatedServerManager.ServerName)
            && _serverDSHub!=null)
            {
                _serverDSHub.Connect(DedicatedServerManager.ServerName);
            }
        };
    }


    public void UnregisterServer()
    {        
        var isLocalServer = ConnectionHandler.GetArgument();
        if (!isLocalServer)
        {
            // Deregister & Shutdown Cloud Server from DSM (ARMADA)
            DedicatedServerManager.ShutdownServer(true, result => OnUnregisterServerComplete(result, false));
        }
        else
        {
            // Deregister Local Server from DSM (ARMADA)
            DedicatedServerManager.DeregisterLocalServer(result => OnUnregisterServerComplete(result, true));
        }   

    }

    #region Callback
    private void OnRegisterServerComplete(Result result, bool isLocalServer)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log(isLocalServer
                ? "Success to Register Local server to DSM"
                : "Success to Register Cloud server to DSM");
        }
        else
        {
            BytewarsLogger.Log(isLocalServer
                ? "Failed to Register Local server to DSM"
                : "Failed to Register Cloud server to DSM");
        } 
        OnRegisterCompleteEvent?.Invoke(result);
    }

    private void OnUnregisterServerComplete(Result result, bool isLocalServer)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log(isLocalServer
                ? "Success to Unregister Local Server from DSM"
                : "Success to Unregister Cloud Server from DSM");
        }
        else
        {
            BytewarsLogger.Log(isLocalServer
                ? "Failed to Register Local server to DSM"
                : "Failed to Register Cloud server to DSM");
        }
        OnUnregisterCompleteEvent?.Invoke(result);
    }

    #endregion
}
#endif
