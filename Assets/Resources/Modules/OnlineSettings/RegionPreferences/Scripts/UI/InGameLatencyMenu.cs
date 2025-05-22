// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;

public class InGameLatencyMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text regionCodeText;
    [SerializeField] private TMP_Text latencyText;

    private RegionPreferencesWrapper regionPreferencesWrapper;

#if !UNITY_SERVER
    private void Awake()
    {
        regionPreferencesWrapper = TutorialModuleManager.Instance.GetModuleClass<RegionPreferencesWrapper>();
    }

    private void OnEnable()
    {
        if (!regionPreferencesWrapper) 
        {
            BytewarsLogger.LogWarning("Unable to initialize in-game latency menu. The region preferences wrapper is null.");
            return;
        }

        if (!regionPreferencesWrapper.ShouldShowInGameLatency())
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        regionPreferencesWrapper.OnInGameLatencyUpdated += UpdateInGameLatency;
        regionPreferencesWrapper.StartInGameLatencyRefresh();
    }

    private void OnDisable()
    {
        if (!regionPreferencesWrapper)
        {
            BytewarsLogger.LogWarning("Unable to deinitialize in-game latency menu. The region preferences wrapper is null.");
            return;
        }

        regionPreferencesWrapper.OnInGameLatencyUpdated -= UpdateInGameLatency;
        regionPreferencesWrapper.StopInGameLatencyRefresh();
    }

    private void UpdateInGameLatency()
    {
        if (!regionPreferencesWrapper)
        {
            BytewarsLogger.LogWarning("Unable to update in-game latency menu. The region preferences wrapper is null.");
            return;
        }

        regionCodeText.text = regionPreferencesWrapper.CurrentServerRegion;
        latencyText.text = $"Ping: {regionPreferencesWrapper.InGameLatency} ms";
    }
#endif
}
