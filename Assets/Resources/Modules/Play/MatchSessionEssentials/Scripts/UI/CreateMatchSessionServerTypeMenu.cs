// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class CreateMatchSessionServerTypeMenu : MenuCanvas
{
    [SerializeField] private Button dedicatedServerButton;
    [SerializeField] private Button peerToPeerButton;
    [SerializeField] private Button backButton;

    ModuleModel matchSessionDSModule, matchSessionP2PModule;

    private void Awake()
    {
        dedicatedServerButton.onClick.AddListener(() => OnDedicatedServerButtonClicked().Forget());
        peerToPeerButton.onClick.AddListener(() => OnPeerToPeerButtonClicked().Forget());
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        matchSessionDSModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionDSEssentials);
        matchSessionP2PModule ??= TutorialModuleManager.Instance.GetModule(TutorialType.MatchSessionP2PEssentials);

        dedicatedServerButton.gameObject.SetActive(matchSessionDSModule?.isActive ?? false);
        peerToPeerButton.gameObject.SetActive(matchSessionP2PModule?.isActive ?? false);
    }

    private async UniTask OnDedicatedServerButtonClicked()
    {
        if (await AccelByteWarsOnlineSession.OnValidateToStartGameSession.Invoke())
        {
            MenuManager.Instance.ChangeToMenu(
                matchSessionDSModule.isStarterActive ? AssetEnum.CreateMatchSessionDSMenu_Starter : AssetEnum.CreateMatchSessionDSMenu);
        }
    }

    private async UniTask OnPeerToPeerButtonClicked()
    {
        if (await AccelByteWarsOnlineSession.OnValidateToStartGameSession.Invoke())
        {
            MenuManager.Instance.ChangeToMenu(
                matchSessionP2PModule.isStarterActive ? AssetEnum.CreateMatchSessionP2PMenu_Starter : AssetEnum.CreateMatchSessionP2PMenu);
        }
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
