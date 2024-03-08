// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class ProfileMenu : MenuCanvas
{
    [SerializeField]
    private Button statsButton;
    [SerializeField]
    private Button backButton;


    private void Start()
    {
        statsButton.onClick.AddListener(OnStatsButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnStatsButtonClicked()
    {
        var statsEssentials = TutorialModuleManager.Instance.GetModule(TutorialType.StatsEssentials);
        if (statsEssentials != null)
        {
            MenuManager.Instance.ChangeToMenu(statsEssentials.mainPrefab.GetAssetEnum());
        }
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return statsButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.ProfileMenuCanvas;
    }
}
