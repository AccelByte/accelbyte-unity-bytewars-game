// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.IO;
using AccelByte.Core;
using UnityEngine;

public class MultiplayerDSEssentialsWrapper_Starter : MonoBehaviour
{
#if UNITY_SERVER

    private IDSService _dsServiceWrapper;
    private bool IsArmadaV3;
    public event Action OnLoginServerCompleteEvent;
    private void Start()
    {
        _dsServiceWrapper = TutorialModuleManager.Instance.GetModuleClass<ArmadaV3Wrapper_Starter>();
        _dsServiceWrapper.OnInstantiateComplete += SwitchModule;
        _dsServiceWrapper.OnInstantiateComplete += _dsServiceWrapper.LoginServer;
        _dsServiceWrapper.OnLoginCompleteEvent += RegisterDS;
        _dsServiceWrapper.OnRegisterCompleteEvent += ConnectToDSHub;
        _dsServiceWrapper.OnRegisterCompleteEvent += OnDisconnectListener;
        GameManager.Instance.OnDeregisterServer += () => _dsServiceWrapper.UnregisterServer();
        _dsServiceWrapper.OnUnregisterCompleteEvent += ShutdownDS;
    }
    
    private void RegisterDS(Result result)
    {
        if (!result.IsError)
        {
            OnLoginServerCompleteEvent?.Invoke();
            _dsServiceWrapper.RegisterServer();
        }
    }

    private void ConnectToDSHub(Result result)
    {
        if (!result.IsError)
        {
            _dsServiceWrapper.ListenOnDisconnect();
        }
    }

    private void OnDisconnectListener(Result result)
    {
        if (!result.IsError)
        {
            _dsServiceWrapper.ConnectToDSHub();
        }
    }

    private void ShutdownDS(Result result)
    {
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Application Quit");
            Application.Quit();
        } 
    }

    private void SwitchModule()
    {
        BytewarsLogger.Log($"AMSWrapper");

        if (CheckDSServerConfig())
        {   
            BytewarsLogger.Log($"AMSWrapper");
            var armadaV3Wrapper = TutorialModuleManager.Instance.GetModuleClass<ArmadaV3Wrapper_Starter>();
            armadaV3Wrapper.gameObject.SetActive(false);
            
            _dsServiceWrapper = TutorialModuleManager.Instance.GetModuleClass<AMSWrapper_Starter>();
            IsArmadaV3 = false;
        }
        else
        {
            BytewarsLogger.Log($"ArmadaV3Wrapper");

            //Disable armada
            var amsWrapper = TutorialModuleManager.Instance.GetModuleClass<AMSWrapper_Starter>();
            amsWrapper.gameObject.SetActive(false);
            
            _dsServiceWrapper = TutorialModuleManager.Instance.GetModuleClass<ArmadaV3Wrapper_Starter>();
            IsArmadaV3 = true;
        }
    }
    
    
    private static bool CheckDSServerConfig()
    {
        var tutorialModuleConfig = (TextAsset)Resources.Load( "Modules/TutorialModuleConfig");
        if (tutorialModuleConfig == null)
        {
            return false;
        }
        
        var json = JsonUtility.FromJson<TutorialModuleConfig>(tutorialModuleConfig.text);
        if (json.multiplayerDSConfiguration == null)
        {
            return false;
        }
        
        return json.multiplayerDSConfiguration.isServerUseAMS;
    }
#endif
}
