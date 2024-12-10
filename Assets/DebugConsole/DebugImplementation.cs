#if BYTEWARS_TUTORIAL
using AccelByte.Core;
#endif
using Debugger;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugImplementation
{
    public DebugImplementation()
    {
        DebugConsole.AddButton("shutdown netcode", OnShutdownNetcode);
        DebugConsole.AddButton("disconnect", OnTestDisconnect);
        #if BYTEWARS_TUTORIAL
        DebugConsole.AddButton("check match session", MatchSessionWrapper.GetDetail);
#if UNITY_SERVER
        DebugConsole.AddButton("Deregister Local Server", OnDisconnectLocalDs);
#endif
#endif
    }
#if BYTEWARS_TUTORIAL
    private void OnDisconnectLocalDs()
    {
        AccelByteSDK.GetServerRegistry().GetApi()
            .GetDedicatedServerManager()
            .DeregisterLocalServer(result =>
            {
                if (result.IsError)
                {
                    Debug.Log("Failed Deregister Local Server");
                }
                else
                {
                    Debug.Log("Successfully Deregister Local Server");
#if UNITY_EDITOR
                    EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                }
            });
    }
    #endif
    private void OnTestDisconnect()
    {
        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.ConnectionData.Port = 1234;
    }

    private void OnShutdownNetcode()
    {
        if (!NetworkManager.Singleton.ShutdownInProgress)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
