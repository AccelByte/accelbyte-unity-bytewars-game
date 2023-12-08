// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

#if UNITY_SERVER

using System;
using AccelByte.Core;
using AccelByte.Server;

public class AMSWrapper_Starter : GameSessionEssentialsWrapper, IDSService
{
    public event Action OnInstantiateComplete;
    public event ResultCallback OnLoginCompleteEvent;
    public event ResultCallback OnRegisterCompleteEvent;
    public event ResultCallback OnUnregisterCompleteEvent;

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
        AccelByteServerPlugin.GetAMS().SendReadyMessage();
    }

    public void ConnectToDSHub()
    {
        BytewarsLogger.Log("Not implemented yet");
    }

    public void ListenOnDisconnect()
    {
        BytewarsLogger.Log("Not implemented yet");
    }

    public void UnregisterServer()
    {
        AccelByteServerPlugin.GetAMS().OnDrainReceived += () =>
        {
            //Drain callback
        };
    }
}
#endif