// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Threading;
using UnityEngine;
using AccelByte.Models;

public class LoginQueueWrapper : MonoBehaviour
{
    public Action<LoginQueueTicket> OnLoginQueued = delegate {};
    public Action OnLoginCanceled = delegate {};
    
    private CancellationTokenSource cancellationTokenSource;
    private AuthEssentialsWrapper authEssentialsWrapper;
    private SinglePlatformAuthWrapper singlePlatformAuthWrapper;
    private AuthEssentialsWrapper_Starter authEssentialsWrapper_Starter;
    private SinglePlatformAuthWrapper_Starter singlePlatformAuthWrapper_Starter;
    
    private void Start()
    {
        authEssentialsWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        singlePlatformAuthWrapper = TutorialModuleManager.Instance.GetModuleClass<SinglePlatformAuthWrapper>();
        authEssentialsWrapper_Starter = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper_Starter>();
        singlePlatformAuthWrapper_Starter = TutorialModuleManager.Instance.GetModuleClass<SinglePlatformAuthWrapper_Starter>();

        // Bind Queued and Canceled action
        if (authEssentialsWrapper != null)
        {
            authEssentialsWrapper.OptionalParameters.OnQueueUpdatedEvent = OnQueued;
            authEssentialsWrapper.OptionalParameters.OnCancelledEvent = OnCanceled;
        }
        if (singlePlatformAuthWrapper != null)
        {
            singlePlatformAuthWrapper.OptionalParameters.OnQueueUpdatedEvent = OnQueued;
            singlePlatformAuthWrapper.OptionalParameters.OnCancelledEvent = OnCanceled;
        }
        
        // Bind Queued and Canceled action to the starter scripts
        if (authEssentialsWrapper_Starter != null)
        {
            authEssentialsWrapper_Starter.OptionalParameters.OnQueueUpdatedEvent = OnQueued;
            authEssentialsWrapper_Starter.OptionalParameters.OnCancelledEvent = OnCanceled;
        }
        if (singlePlatformAuthWrapper_Starter != null)
        {
            singlePlatformAuthWrapper_Starter.OptionalParameters.OnQueueUpdatedEvent = OnQueued;
            singlePlatformAuthWrapper_Starter.OptionalParameters.OnCancelledEvent = OnCanceled;
        }
        
        ResetAndReassignCancellationToken();
    }

    private void ResetAndReassignCancellationToken()
    {
        cancellationTokenSource = new CancellationTokenSource();
        
        // Set cancellation token
        if (authEssentialsWrapper != null)
        {
            authEssentialsWrapper.OptionalParameters.CancellationToken = cancellationTokenSource.Token;
        }
        if (singlePlatformAuthWrapper != null)
        {
            singlePlatformAuthWrapper.OptionalParameters.CancellationToken = cancellationTokenSource.Token;
        }
        
        // Set cancellation token to the starter scripts
        if (authEssentialsWrapper_Starter != null)
        {
            authEssentialsWrapper_Starter.OptionalParameters.CancellationToken = cancellationTokenSource.Token;
        }
        if (singlePlatformAuthWrapper_Starter != null)
        {
            singlePlatformAuthWrapper_Starter.OptionalParameters.CancellationToken = cancellationTokenSource.Token;
        }
    }

    public void CancelLogin()
    {
        cancellationTokenSource.Cancel();
    }

    private void OnQueued(LoginQueueTicket queueTicket)
    {
        BytewarsLogger.Log($"Login queued.");
        
        // Show UI if the current UI is not Login Queue
        if (MenuManager.Instance.GetCurrentMenu().GetAssetEnum() != AssetEnum.LoginQueueMenu)
        {
            MenuManager.Instance.ChangeToMenu(TutorialType.LoginQueue);
        }
        
        OnLoginQueued?.Invoke(queueTicket);
    }

    private void OnCanceled()
    {
        BytewarsLogger.Log($"Login canceled while in queue.");
        
        OnLoginCanceled?.Invoke();
        ResetAndReassignCancellationToken();
    }
}