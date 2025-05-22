// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RegionPreferencesEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text regionNameText;
    [SerializeField] private TMP_Text regionCodeText;
    [SerializeField] private TMP_Text latencyText;
    [SerializeField] private TMP_Text optButtonText;
    [SerializeField] private Button optButton;

#if !UNITY_SERVER
    public void Init(RegionPreferenceInfo regionInfo) 
    {
        if (regionInfo == null) 
        {
            BytewarsLogger.LogWarning("Unable to initialize region preference entry. Region preference info is null.");
            return;
        }

        DisplayPreferenceRegionInfo(regionInfo);

        optButton.onClick.RemoveAllListeners();
        optButton.onClick.AddListener(() => ToggleOptRegion(regionInfo));
    }

    private void DisplayPreferenceRegionInfo(RegionPreferenceInfo regionInfo)
    {
        if (regionInfo == null)
        {
            BytewarsLogger.LogWarning("Unable to display region preference info. Region preference info is null.");
            return;
        }

        RegionPreferenceInfo.RegionNames.TryGetValue(regionInfo.RegionCode, out string regionName);
        regionNameText.text = string.IsNullOrEmpty(regionName) ? "Unknown" : regionName;
        regionCodeText.text = regionInfo.RegionCode;
        latencyText.text = $"{regionInfo.Latency} ms";
        optButtonText.text = regionInfo.Enabled ? "Opt Out" : "Opt In";
    }

    private void ToggleOptRegion(RegionPreferenceInfo regionInfo)
    {
        RegionPreferencesWrapper regionPreferencesWrapper = TutorialModuleManager.Instance.GetModuleClass<RegionPreferencesWrapper>();
        if (regionPreferencesWrapper == null) 
        {
            BytewarsLogger.LogWarning("Unable to toggle opt region preferences. The wrapper is null.");
            return;
        }

        // Toggle opt region.
        if (regionPreferencesWrapper.ToggleEnableRegion(regionInfo.RegionCode)) 
        {
            // Display updated region preference info.
            DisplayPreferenceRegionInfo(regionPreferencesWrapper.FindRegionInfo(regionInfo.RegionCode));
        }
    }
#endif
}
