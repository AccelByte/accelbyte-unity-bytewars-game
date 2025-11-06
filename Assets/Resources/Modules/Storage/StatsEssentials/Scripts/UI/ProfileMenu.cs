// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileMenu : MenuCanvas
{
    [SerializeField] private AccelByteWarsAsyncImage profileImage;
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text userIdText;
    [SerializeField] private TMP_Text platformText;
    [SerializeField] private Button copyUserIdButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        copyUserIdButton.onClick.AddListener(() => AccelByteWarsUtility.CopyToClipboard(GameData.CachedPlayerState?.PlayerId));
        statsButton.onClick.AddListener(OnStatsButtonClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    void OnEnable()
    {
        if (GameData.CachedPlayerState != null)
        {
            profileImage.LoadImage(GameData.CachedPlayerState.AvatarUrl);
            displayNameText.text = GameData.CachedPlayerState.PlayerName;
            userIdText.text = $"User ID: {GameData.CachedPlayerState.PlayerId}";
            platformText.text = $"Platform: {GameData.CachedPlayerState.PlatformId}";
        }
    }

    private void OnStatsButtonClicked()
    {
        ModuleModel statsEssentials = TutorialModuleManager.Instance.GetModule(TutorialType.StatsEssentials);
        MenuManager.Instance.ChangeToMenu(statsEssentials.isStarterActive ? AssetEnum.StatsProfileMenu_Starter : AssetEnum.StatsProfileMenu);
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
        return AssetEnum.ProfileMenu;
    }
}
