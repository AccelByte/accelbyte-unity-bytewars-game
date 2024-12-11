// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class CrossPlayPreferencesMenu : MenuCanvas
{
    [Header("Components")]
    [SerializeField] private RectTransform loadingPanel;
    [SerializeField] private RectTransform savingPanel;
    [SerializeField] private RectTransform failedPanel;
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private Button backButton;

    [Header("Menu")]
    [SerializeField] private Toggle crossPlayToggle;

    private CrossPlayPreferencesWrapper crossPlayPreferencesWrapper;

    private enum CrossplayPreferencesView
    {
        Loading,
        Saving,
        Failed,
        Success
    }

    private CrossplayPreferencesView currentView = CrossplayPreferencesView.Loading;

    private CrossplayPreferencesView CurrentView
    {
        get => currentView;
        set
        {
            loadingPanel.gameObject.SetActive(value == CrossplayPreferencesView.Loading);
            savingPanel.gameObject.SetActive(value == CrossplayPreferencesView.Saving);
            failedPanel.gameObject.SetActive(value == CrossplayPreferencesView.Failed);
            contentPanel.gameObject.SetActive(value == CrossplayPreferencesView.Success);
            currentView = value;
        }
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CrossPlayPreferencesMenu;
    }

    private void Awake()
    {
        backButton.onClick.AddListener(SavePreferences);
    }

    private void OnEnable()
    {
        crossPlayPreferencesWrapper ??= TutorialModuleManager.Instance.GetModuleClass<CrossPlayPreferencesWrapper>();
        if (crossPlayPreferencesWrapper == null) 
        {
            return;
        }

        GetPreferences();
    }

    private void GetPreferences()
    {
        CurrentView = CrossplayPreferencesView.Loading;
        crossPlayPreferencesWrapper.GetPlayerSessionAttribute((Result<PlayerAttributesResponseBody> result) => 
        {
            if (result.IsError)
            {
                CurrentView = CrossplayPreferencesView.Failed;
                return;
            }

            crossPlayToggle.isOn = result.Value.CrossPlayEnabled;
            CurrentView = CrossplayPreferencesView.Success;
        });
    }

    private void SavePreferences() 
    {
        CurrentView = CrossplayPreferencesView.Saving;

        PlayerAttributesRequestBody request = new PlayerAttributesRequestBody()
        {
            CrossPlayEnabled = crossPlayToggle.isOn
        };
        crossPlayPreferencesWrapper.StorePlayerSessionAttribute(request, (Result<PlayerAttributesResponseBody> result) =>
        {
            MenuManager.Instance.OnBackPressed();
        });
    }
}
