// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using AccelByte.Models;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoginQueueMenu : MenuCanvas
{
    #region UI Component
    [SerializeField] private GameObject inQueuePanel;
    [SerializeField] private GameObject cancelingPanel;
    [SerializeField] private TMP_Text positionInQueueText;
    [SerializeField] private TMP_Text estimatedWaitingTimeText;
    [SerializeField] private TMP_Text lastUpdateTimeText;
    [SerializeField] private Button cancelLoginButton;

    private enum UIState
    {
        InQueue,
        Canceling
    }

    private UIState CurrentView
    {
        get => CurrentView;
        set
        {
            switch (value)
            {
                case UIState.InQueue:
                    inQueuePanel.SetActive(true);
                    cancelingPanel.SetActive(false);
                    StartCoroutine(SetSelectedGameObject(cancelLoginButton.gameObject));
                    break;
                case UIState.Canceling:
                    inQueuePanel.SetActive(false);
                    cancelingPanel.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    #endregion

    private LoginQueueWrapper loginQueueWrapper;
    
    public override GameObject GetFirstButton()
    {
        return cancelLoginButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LoginQueueMenu;
    }
    
    IEnumerator SetSelectedGameObject(GameObject gameObjectToSelect)
    {
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(gameObjectToSelect);
    }

    private void Start()
    {
        // Get wrapper reference.
        loginQueueWrapper = TutorialModuleManager.Instance.GetModuleClass<LoginQueueWrapper>();
        
        // Bind to Login Queue delegates.
        loginQueueWrapper.OnLoginQueued = LoginQueued;
        loginQueueWrapper.OnLoginCanceled = LoginCanceled;
        
        // Bind button.
        cancelLoginButton.onClick.AddListener(CancelLogin);
    }

    private void LoginQueued(LoginQueueTicket loginQueueTicket)
    {
        CurrentView = UIState.InQueue;
        SetPositionText(loginQueueTicket.Position);
        SetEstimatedWaitingTimeText(loginQueueTicket.EstimatedWaitingTimeInSeconds);
        SetLastUpdateTimeText();
    }

    private void CancelLogin()
    {
        CurrentView = UIState.Canceling;

        loginQueueWrapper.CancelLogin();
    }
    
    private void LoginCanceled()
    {
        // Go back to the previous menu
        MenuManager.Instance.OnBackPressed();
    }

    #region UI Utilities
    private void SetPositionText(int position)
    {
        positionInQueueText.SetText($"{position}");
    }

    private void SetEstimatedWaitingTimeText(int timeInSeconds)
    {
        estimatedWaitingTimeText.SetText($"{timeInSeconds} Seconds");
    }

    private void SetLastUpdateTimeText()
    {
        lastUpdateTimeText.SetText(DateTime.Now.ToString("HH:mm:ss"));
    }
    #endregion
}