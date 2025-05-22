// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionPreferencesMenu : MenuCanvas
{
    [Header("Region Preference Components")]
    [SerializeField] private RegionPreferencesEntry entryPrefab;
    [SerializeField] private RectTransform emptyPanel;
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform failedPanel;
    [SerializeField] private RectTransform resultContentPanel;

    [Header("Menu Components")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;

    [SerializeField] private float warningDisplayTime = 5.0f;
    private Coroutine warningDisplayCoroutine;

    private RegionPreferencesWrapper regionPreferencesWrapper;

    private enum RegionPreferencesView
    {
        Empty,
        Loading,
        Failed,
        Success
    }

    private RegionPreferencesView currentView = RegionPreferencesView.Empty;

    private RegionPreferencesView CurrentView
    {
        get => currentView;
        set
        {
            emptyPanel.gameObject.SetActive(value == RegionPreferencesView.Empty);
            loadingPanel.gameObject.SetActive(value == RegionPreferencesView.Loading);
            failedPanel.gameObject.SetActive(value == RegionPreferencesView.Failed);
            resultContentPanel.gameObject.SetActive(value == RegionPreferencesView.Success);
            currentView = value;
        }
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.RegionPreferencesMenu;
    }

#if !UNITY_SERVER
    private void Awake()
    {
        refreshButton.onClick.AddListener(QueryRegionPreferences);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable()
    {
        warningText.gameObject.SetActive(false);

        if (regionPreferencesWrapper == null)
        {
            regionPreferencesWrapper = TutorialModuleManager.Instance.GetModuleClass<RegionPreferencesWrapper>();
        }

        if (!regionPreferencesWrapper) 
        {
            return;
        }

        regionPreferencesWrapper.OnMinimumRegionCountWarning += OnMinimumRegionCountWarning;

        if (ApiClientHelper.IsPlayerLoggedIn) 
        {
            if (regionPreferencesWrapper.GetRegionInfos().Count > 0) 
            {
                DisplayRegionPreferences();
            }
            else 
            {
                QueryRegionPreferences();
            }
        }
        else 
        {
            BytewarsLogger.LogWarning("Unable to load region preferences. Player has not logged in yet.");
            CurrentView = RegionPreferencesView.Failed;
        }
    }

    private void OnDisable()
    {
        if (!regionPreferencesWrapper)
        {
            return;
        }

        regionPreferencesWrapper.OnMinimumRegionCountWarning -= OnMinimumRegionCountWarning;
    }

    private void QueryRegionPreferences() 
    {
        refreshButton.enabled = false;
        CurrentView = RegionPreferencesView.Loading;
        regionPreferencesWrapper.QueryRegionLatencies(OnQueryRegionPreferencesComplete);
    }

    private void OnQueryRegionPreferencesComplete(Result<Dictionary<string, int>> result)
    {
        refreshButton.enabled = true;

        if (result.IsError)
        {
            CurrentView = RegionPreferencesView.Failed;
            return;
        }

        DisplayRegionPreferences();
    }

    private void DisplayRegionPreferences()
    {
        refreshButton.enabled = true;

        List<RegionPreferenceInfo> regionInfos = regionPreferencesWrapper.GetRegionInfos();
        if (regionInfos.Count <= 0) 
        {
            CurrentView = RegionPreferencesView.Empty;
            return;
        }

        resultContentPanel.DestroyAllChildren();
        foreach (RegionPreferenceInfo regionInfo in regionInfos)
        {
            RegionPreferencesEntry newEntry = Instantiate(entryPrefab, resultContentPanel);
            newEntry.Init(regionInfo);
        }

        CurrentView = RegionPreferencesView.Success;
    }

    private void OnMinimumRegionCountWarning() 
    {
        if (warningDisplayCoroutine != null)
        {
            StopCoroutine(warningDisplayCoroutine);
        }
        warningDisplayCoroutine = StartCoroutine(DisplayMinimumRegionCountWarning());
    }

    private IEnumerator DisplayMinimumRegionCountWarning() 
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(warningDisplayTime);
        warningText.gameObject.SetActive(false);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
#endif
}
