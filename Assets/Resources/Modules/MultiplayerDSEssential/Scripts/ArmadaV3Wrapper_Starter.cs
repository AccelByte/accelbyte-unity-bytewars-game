// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.
#if UNITY_SERVER

using System;
using AccelByte.Core;
using AccelByte.Server;

public class ArmadaV3Wrapper_Starter : GameSessionEssentialsWrapper, IDSService
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
    }
    
    public void RegisterServer()
    {
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

    }

    #region Callback


    #endregion
}
#endif
