// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Text;
using AccelByte.Core;
using AccelByte.Models;
using AccelByte.Server;
using Unity.Profiling;

public class MultiplayerDSArmadaWrapper: GameSessionEssentialsWrapper
{
    public event Action OnInstantiateComplete;
    public event ResultCallback OnLoginCompleteEvent;
    public event ResultCallback OnRegisterCompleteEvent;
    public event ResultCallback OnUnregisterCompleteEvent;
    
    private DedicatedServer ds;
    private DedicatedServerManager dsm;
    private ServerDSHub dsHub;
    
    private bool isLocalServer = false;
    
#if UNITY_SERVER

    public string DedicatedServerName => dsm.ServerName;

    private void Start()
    {
        ds = AccelByteServerPlugin.GetDedicatedServer();
        dsm = MultiRegistry.GetServerApiClient().GetDedicatedServerManager();
        dsHub = MultiRegistry.GetServerApiClient().GetDsHub();
        
        ConnectionHandler.Initialization();
        isLocalServer = ConnectionHandler.IsUsingLocalDS();
    }
    
    #region Events Functions
    
    public void SubscribeDSHubEvents()
    {
        dsHub.OnConnected += OnDSHubConnected;
        dsHub.OnDisconnected += OnDSHubDisconnected;
    }
    
    private void OnDSHubConnected()
    {
        BytewarsLogger.Log($"Success to log in {dsm.ServerName} to DS Hub");
    }

    private void OnDSHubDisconnected(WsCloseCode wsCloseCode)
    {
        BytewarsLogger.Log("disconnected from server ds hub, try to reconnect");
        if (!String.IsNullOrEmpty(dsm.ServerName) && dsHub != null)
        {
            dsHub.Connect(dsm.ServerName);
        }
    }
    
    #endregion
    
    #region AB Service Functions
    
    public void LoginWithClientCredentials(ResultCallback resultCallback = null)
    {
        ds?.LoginWithClientCredentials(
            result => OnLoginWithClientCredentialsCompleted(result, resultCallback)
        );
    }
    
    public void RegisterServer(ResultCallback resultCallback = null)
    {
        int port = ConnectionHandler.GetPort();
        
        if (!isLocalServer)
        {
            dsm?.RegisterServer(
                port, 
                result => OnRegisterServerCompleted(result, resultCallback) 
            );
        }
        else
        {
            string ip = ConnectionHandler.LocalServerIP;
            string serverName = ConnectionHandler.LocalServerName;
            
            dsm?.RegisterLocalServer(
                ip,
                (uint)port,
                serverName,
                result => OnRegisterServerCompleted(result, resultCallback)
            );
        }
    }
    
    public void ConnectToDSHub(string serverName)
    {
        dsHub?.Connect(serverName);
    }
    
    public void UnregisterServer(ResultCallback resultCallback = null)
    {
        if (!isLocalServer)
        {
            dsm?.ShutdownServer(
                true, 
                result => OnUnregisterServerCompleted(result, resultCallback)
            );
        }
        else
        {
            dsm?.DeregisterLocalServer(
                result => OnUnregisterServerCompleted(result, resultCallback)
            );
        }
    }
    
    #endregion
    
    #region Callback Functions
    
    private void OnLoginWithClientCredentialsCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Server login success.");
        }
        else
        {
            BytewarsLogger.Log($"Server login failed. Error code: {result.Error.Code}, message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    private void OnRegisterServerCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Register server to DSM success.");
        }
        else
        {
            BytewarsLogger.Log($"Failed to Register server to DSM. Error code: {result.Error.Code}, message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    private void OnUnregisterServerCompleted(Result result, ResultCallback customCallback = null)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log("Unregister Server from DSM success");
        }
        else
        {
            BytewarsLogger.Log($"Failed to Unregister server from DSM. Error code: {result.Error.Code}, message: {result.Error.Message}");
        }
        
        customCallback?.Invoke(result);
    }
    
    #endregion

#endif
}