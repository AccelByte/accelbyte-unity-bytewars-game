// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class MatchmakingServerTypeMenu : MenuCanvas
{
    [SerializeField] private Button dedicatedServerButton;
    [SerializeField] private Button peerToPeerButton;
    [SerializeField] private Button backButton;

    ModuleModel matchmakingDSModule, matchmakingP2PModule;

    private void Awake()
    {
        dedicatedServerButton.onClick.AddListener(OnDedicatedServerButtonClicked);
        peerToPeerButton.onClick.AddListener(OnPeerToPeerButtonClicked);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        matchmakingDSModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchmakingDSEssentials);
        matchmakingP2PModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchmakingP2PEssentials);

        dedicatedServerButton.gameObject.SetActive(matchmakingDSModule?.isActive ?? false);
        peerToPeerButton.gameObject.SetActive(matchmakingP2PModule?.isActive ?? false);
    }

    private void OnDedicatedServerButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(
            matchmakingDSModule.isStarterActive ? AssetEnum.MatchmakingDSMenu_Starter : AssetEnum.MatchmakingDSMenu);
    }

    private void OnPeerToPeerButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(
            matchmakingP2PModule.isStarterActive ? AssetEnum.MatchmakingP2PMenu_Starter : AssetEnum.MatchmakingP2PMenu);
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.MatchmakingServerTypeMenu;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
