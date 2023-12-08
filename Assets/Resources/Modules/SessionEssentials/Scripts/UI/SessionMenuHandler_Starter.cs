using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionMenuHandler_Starter : MenuCanvas
{
    [SerializeField] private Button createEliminationButton;
    [SerializeField] private Button backButton;    
    [SerializeField] private Button backFailedButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button cancelButton;
    
    [SerializeField] private RectTransform defaultPanel;
    [SerializeField] private RectTransform creatingPanel;
    [SerializeField] private RectTransform joiningPanel;
    [SerializeField] private RectTransform joinedPanel;
    [SerializeField] private RectTransform failedPanel;
    
    [SerializeField] private RectTransform footerPanel;
    
    private List<RectTransform> _panels = new List<RectTransform>();

    private SessionRequestPayload _sessionRequestPayload;
    
    private SessionResponsePayload _sessionResponsePayload;
    
    // Copy SessionEssentialsWrapper_Starter field from "Putting it all together" unit here (step number 1)
    
    private static readonly TutorialType _tutorialType = TutorialType.SessionEssentials;

    public enum SessionMenuView
    {
        Default,
        Creating,
        Failed,
        Joining,
        Joined
    }

    private SessionMenuView CurrentView
    {
        get => CurrentView;
        set => viewSwitcher(value);
    }

    private void viewSwitcher(SessionMenuView value)
    {
        switch (value)
        {
            case SessionMenuView.Default:
                switcherHelper(defaultPanel, value);
                break;
            case SessionMenuView.Creating:
                switcherHelper(creatingPanel, value);
                break;
            case SessionMenuView.Failed:
                switcherHelper(failedPanel, value);
                break;
            case SessionMenuView.Joining:
                switcherHelper(joiningPanel, value);
                break;
            case SessionMenuView.Joined:
                switcherHelper(joinedPanel, value);
                break;
        }
    }

    private void switcherHelper(RectTransform panel, SessionMenuView value)
    {
        panel.gameObject.SetActive(true);
        _panels.Except(new []{panel})
            .ToList().ForEach(x => x.gameObject.SetActive(false));
        if (value != SessionMenuView.Default)
        {
            footerPanel.gameObject.SetActive(false);
            return;
        }
        
        footerPanel.gameObject.SetActive(true);
    }

    private void Awake()
    {
        _panels = new List<RectTransform>()
        {
            defaultPanel,
            creatingPanel,
            joiningPanel,
            joinedPanel,
            failedPanel
        };
        
        // Copy _sessionEssentialsWrapper variable assignment from "Putting it all together" unit here (step number 2)
    }
    
    // Update OnEnable() with the code snippet from "Putting it all together" unit here (step number 5)
    private void OnEnable()
    {
        CurrentView = SessionMenuView.Default;
        
    }

    // Update OnDisable() with the code snippet from "Putting it all together" unit here (step number 6)
    private void OnDisable()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        // Copy if condition from "Putting it all together" unit here (step number 4)

        createEliminationButton.onClick.AddListener(OnEliminationButtonClicked);
        leaveButton.onClick.AddListener(OnLeaveSessionButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        backFailedButton.onClick.AddListener(OnBackFailedButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackFailedButtonClicked()
    {
        CurrentView = SessionMenuView.Default;
    }
    
    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    private void OnCancelButtonClicked()
    {
        StopAllCoroutines();
        MenuManager.Instance.OnBackPressed();
    }
    
    // Update OnEliminationButtonClicked() with the code snippet from "Putting it all together" unit here (step number 7)
    private void OnEliminationButtonClicked()
    {
    }

    // Update OnLeaveSessionButtonClicked() with the code snippet from "Putting it all together" unit here (step number 8)
    private void OnLeaveSessionButtonClicked()
    {
    }
    
    #region EventCallback

    // Update OnCreateSessionCompleted() with the code snippet from "Putting it all together" unit here (step number 8)
    private void OnCreateSessionCompleted(SessionResponsePayload response)
    {
    }
    
    // Update OnLeaveSessionCompleted() with the code snippet from "Putting it all together" unit here (step number 8)
    private void OnLeaveSessionCompleted(SessionResponsePayload response)
    {
    }
    
    private IEnumerator DelayCallback(Action<SessionMenuView> action)
    {
        yield return new WaitForSeconds(1);
        action?.Invoke(SessionMenuView.Joining);
        yield return new WaitForSeconds(1);
        action?.Invoke(SessionMenuView.Joined);
    }

    #endregion

    // Copy BindToWrapperEvent() and UnbindToWrapperEvent() from "Putting it all together" unit here (step number 3)

    #region HelperFunction

    private void Helper(SessionResponsePayload response, SessionMenuView sessionMenuView)
    {
        if (!response.IsError)
        {
            switch (sessionMenuView)
            {
                case SessionMenuView.Joining:
                    CurrentView = SessionMenuView.Joining;
                    BytewarsLogger.Log($"{response.Result.Value.id}");
                    _sessionResponsePayload = response;
                    break;
                case SessionMenuView.Joined:
                    CurrentView = SessionMenuView.Joined;
                    BytewarsLogger.Log($"{response.Result.Value.id}");
                    var text = joinedPanel.gameObject.GetComponentInChildren<TMP_Text>();
                    text.text = $"joined session {_sessionResponsePayload.Result.Value.id}";
                    break;
            }
        }
        else
        {
            CurrentView = SessionMenuView.Failed;
            Debug.Log($"{JsonUtility.ToJson(response.Result.Value)}");
        }
    }
    
    private void IsTutorialTypeMatch(TutorialType? tutorialType)
    {
        if (_tutorialType != tutorialType)
        {
            BytewarsLogger.LogWarning($"{tutorialType} is not match with {_tutorialType}");
            return;
        }
        BytewarsLogger.Log($"{tutorialType} is match with {_tutorialType}");
    }
    
    private IEnumerator DelayToJoinedSessionPanel()
    {
        yield return new WaitForSeconds(2);
        CurrentView = SessionMenuView.Joined;
    }
    
    #endregion


    public override GameObject GetFirstButton()
    {
        return createEliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SessionEssentialsMenuCanvas;
    }
    
}
