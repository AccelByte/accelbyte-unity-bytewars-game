// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingMenuCanvas : MenuCanvas
{
    [SerializeField]
    private Image[] loadingImages;
    [SerializeField]
    private Button cancelBtn;
    [SerializeField]
    private Button okBtn;
    [SerializeField]
    private TextMeshProUGUI infoText;
    [SerializeField]
    private GameObject timeoutContainer;
    [SerializeField]
    private TextMeshProUGUI timeoutInfo;
    [SerializeField]
    private TextMeshProUGUI additionalInfo;
    private float oneSecondCounter = 0;
    private int index = 0;
    private int loadingTimeoutSec = 0;
    private string timeoutPrefix;
    private string timeoutReachedInfo;
    private const float animationSpeed = 0.65f;
    private UnityAction cancelProcessCallback;
    private UnityAction okProcessCallback;
    private bool dropAlpha;
    private const string okButtonDefaultText = "Ok";
    private const string cancelButtonDefaultText = "Cancel"; 


    private void StartAnimate()
    {
        index = 0;
        for (int i = 0; i < loadingImages.Length; i++)
        {
            loadingImages[i].color = new Color(1, 1, 1, 0);
            float tempI = i;
            Invoke("AnimateImage", tempI * animationSpeed);
        }
    }

    private void AnimateImage()
    {
        ;
        if (index == loadingImages.Length - 1)
        {
            LeanTween.color(loadingImages[index].rectTransform, Color.white, animationSpeed)
                .setLoopCount(2).setLoopType(LeanTweenType.pingPong)
                .setEaseOutQuad()
                .setOnComplete(StartAnimate);
        }
        else
        {
            LeanTween.color(loadingImages[index].rectTransform, Color.white, animationSpeed)
                .setEaseOutQuad()
                .setLoopCount(2).setLoopType(LeanTweenType.pingPong);
            index++;
        }
    }

    public override GameObject GetFirstButton()
    {
        if (cancelBtn.gameObject.activeSelf)
            return cancelBtn.gameObject;
        else
            return null;
    }

    public void ShowAdditionalInfo(string additionalLoadingInfo, bool hideButtons)
    {
        if (hideButtons)
        {
            okBtn.gameObject.SetActive(false);
            cancelBtn.gameObject.SetActive(false);
        }

        additionalInfo.gameObject.SetActive(true);
        additionalInfo.text = additionalLoadingInfo;
    }

    public void HideAdditionalInfo()
    {
        additionalInfo.gameObject.SetActive(false);
        additionalInfo.text = string.Empty;
    }
    
    public void Show(
        string loadingInfo, 
        bool showCancelButton = true, 
        LoadingTimeoutInfo loadingTimeoutInfo = null, 
        UnityAction cancelCallback = null,
        UnityAction okCallback = null, 
        bool showOkButton = true, 
        string okButtonText = "Ok",
        string cancelButtonText = "Cancel")
    {
        HideAdditionalInfo();
        cancelBtn.onClick.RemoveAllListeners();
        okBtn.onClick.RemoveAllListeners();

        cancelProcessCallback = cancelCallback;
        infoText.text = loadingInfo;

        if (okCallback != null && showOkButton)
        {
            okBtn.onClick.RemoveAllListeners();
            okBtn.gameObject.SetActive(true);
            okBtn.onClick.AddListener(okCallback);

            TMP_Text buttonText = okBtn.GetComponentInChildren<TMP_Text>();
            buttonText.text = string.IsNullOrEmpty(okButtonText) ? okButtonDefaultText : okButtonText;
        }
        else
        {
            okBtn.gameObject.SetActive(false);
        }

        if (cancelCallback != null && showCancelButton)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.gameObject.SetActive(true);
            cancelBtn.onClick.AddListener(cancelCallback);

            TMP_Text buttonText = cancelBtn.GetComponentInChildren<TMP_Text>();
            buttonText.text = string.IsNullOrEmpty(cancelButtonText) ? cancelButtonDefaultText : cancelButtonText;

        }
        else
        {
            cancelBtn.gameObject.SetActive(false);
        }

        if (loadingTimeoutInfo == null)
        {
            timeoutContainer.gameObject.SetActive(false);
        }
        else
        {
            timeoutContainer.gameObject.SetActive(true);
            loadingTimeoutSec = loadingTimeoutInfo.TimeoutSec;
            timeoutPrefix = loadingTimeoutInfo.Info;
            timeoutReachedInfo = loadingTimeoutInfo.TimeoutReachedError;
            UpdateTimeoutLabel();
            oneSecondCounter = 0;
        }
    }

    private void UpdateTimeoutLabel()
    {
        timeoutInfo.text = timeoutPrefix + loadingTimeoutSec;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LoadingMenuCanvas;
    }

    private void FixedUpdate()
    {
        flashText();
        if (timeoutContainer != null && timeoutContainer.activeSelf)
        {
            if (loadingTimeoutSec > 0)
            {
                oneSecondCounter += Time.fixedDeltaTime;
                if (oneSecondCounter >= 1)
                {
                    oneSecondCounter = 0;
                    loadingTimeoutSec--;
                    UpdateTimeoutLabel();
                }
            }
            else
            {
                gameObject.SetActive(false);
                MenuManager.Instance.ShowInfo(timeoutReachedInfo, "Timeout");
                cancelProcessCallback?.Invoke();
            }
        }
    }

private void flashText()
{
    // Adjust alpha based on the current direction of change
    additionalInfo.alpha += (dropAlpha ? -1 : 1) * Time.deltaTime;

    // Clamp alpha between 0 and 1
    additionalInfo.alpha = Mathf.Clamp01(additionalInfo.alpha);

    // Toggle dropAlpha based on alpha value
    if (additionalInfo.alpha == 0 || additionalInfo.alpha == 1)
    {
        dropAlpha = !dropAlpha;
    }
}
}


public class LoadingTimeoutInfo
{
    public string Info;
    public string TimeoutReachedError;
    public int TimeoutSec;
}