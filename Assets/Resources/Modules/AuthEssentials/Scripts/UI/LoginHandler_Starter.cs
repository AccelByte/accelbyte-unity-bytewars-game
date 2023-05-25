using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginHandler_Starter : MonoBehaviour
{
    //Declare each view panels
    [SerializeField] private GameObject loginStatePanel;
    [SerializeField] private GameObject loginLoadingPanel;
    [SerializeField] private GameObject loginFailedPanel;
    
    // Declare UI button here
    [SerializeField] private Button loginWithDeviceIdButton;
    [SerializeField] private Button retryLoginButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private TMP_Text failedMessageText;
    
    //Paste AuthEssentialsSubsystem_Starter from "Put it All together" unit here (step number 3)
    //Paste LoginType from "Put it All together" unit unit here (step number 4)
    
    
    #region LoginView enum
    public enum LoginView
    {
        LoginState,
        LoginLoading,
        LoginFailed
    }
    
    private LoginView CurrentView
    {
        get => CurrentView;
        set
        {
            switch (value)
            {
                case LoginView.LoginState:
                    loginStatePanel.SetActive(true);
                    loginLoadingPanel.SetActive(false);
                    loginFailedPanel.SetActive(false);
                    break;

                case LoginView.LoginLoading:
                    loginStatePanel.SetActive(false);
                    loginLoadingPanel.SetActive(true);
                    loginFailedPanel.SetActive(false);
                    break;
            
                case LoginView.LoginFailed:
                    loginStatePanel.SetActive(false);
                    loginLoadingPanel.SetActive(false);
                    loginFailedPanel.SetActive(true);
                    break;
            }
        }
    }

    #endregion

    //Paste Start() from "Put it All together" unit here (step number 3)

    
    //Paste OnEnable() function from "Add a Login Menu" here (step number 4)

    
    //initially Paste Login() function login from "Add a Login Menu" here (step number 5)
    //then change it using code from "Put it All together" unit (step number 5)

    
    //Paste all callback function from "Add a Login Menu" here (step number 5)
    //Then update OnLoginWithDeviceIdButtonClicked and OnRetryLoginButtonClicked from "Put it All together" unit (step number 6)
    private void OnQuitGameButtonClicked()
    {
        Application.Quit();
    }

    
    
    //Paste OnLoginCompleted using snippet from "Put it All together" unit here (step number 2)



}
