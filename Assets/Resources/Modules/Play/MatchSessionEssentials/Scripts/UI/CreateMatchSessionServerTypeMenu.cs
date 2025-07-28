// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;
using UnityEngine.UI;

public class CreateMatchSessionServerTypeMenu : MenuCanvas
{
    [SerializeField] private Button dedicatedServerButton;
    [SerializeField] private Button peerToPeerButton;
    [SerializeField] private Button backButton;

    ModuleModel matchSessionDSModule, matchSessionP2PModule;

    private void Awake()
    {
        dedicatedServerButton.onClick.AddListener(OnDedicatedServerButtonClicked);
        peerToPeerButton.onClick.AddListener(OnPeerToPeerButtonClicked);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        matchSessionDSModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionDSEssentials);
        matchSessionP2PModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionP2PEssentials);

        dedicatedServerButton.gameObject.SetActive(matchSessionDSModule?.isActive ?? false);
        peerToPeerButton.gameObject.SetActive(matchSessionP2PModule?.isActive ?? false);
    }

    private void OnDedicatedServerButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(
            matchSessionDSModule.isStarterActive ? AssetEnum.CreateMatchSessionDSMenu_Starter : AssetEnum.CreateMatchSessionDSMenu);
    }

    private void OnPeerToPeerButtonClicked()
    {
        MenuManager.Instance.ChangeToMenu(
            matchSessionP2PModule.isStarterActive ? AssetEnum.CreateMatchSessionP2PMenu_Starter : AssetEnum.CreateMatchSessionP2PMenu);
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionServerTypeMenu;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
