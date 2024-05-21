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
    private TextMeshProUGUI infoText;
    [SerializeField]
    private GameObject timeoutContainer;
    [SerializeField]
    private TextMeshProUGUI timeoutInfo;
    private float oneSecondCounter = 0;
    private int index = 0;
    private int loadingTimeoutSec = 0;
    private string timeoutPrefix;
    private string timeoutReachedInfo;
    private const float animationSpeed = 0.65f;
    private UnityAction cancelProcessCallback;

    // Start is called before the first frame update
    void Start()
    {
        StartAnimate();
    }

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

    public void Show(string loadingInfo, LoadingTimeoutInfo loadingTimeoutInfo = null, UnityAction cancelCallback = null)
    {
        cancelBtn.onClick.RemoveAllListeners();

        cancelProcessCallback = cancelCallback;
        infoText.text = loadingInfo;
        if (cancelCallback != null)
        {
            cancelBtn.gameObject.SetActive(true);
            cancelBtn.onClick.AddListener(cancelCallback);
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
}

public class LoadingTimeoutInfo
{
    public string Info;
    public string TimeoutReachedError;
    public int TimeoutSec;
}