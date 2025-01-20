// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class HelpAndOptionsMenu : MenuCanvas
{
    [SerializeField] private Button helpButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button onlineSettingsButton;
    [SerializeField] private Button backButton;

    // Start is called before the first frame update
    void Start()
    {
        
        helpButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.HelpMenuCanvas);
        }));
        optionsButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.OptionsMenuCanvas);
        }));
        creditsButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.CreditsMenuCanvas);
        }));
        onlineSettingsButton.onClick.AddListener((() =>
        {
            MenuManager.Instance.ChangeToMenu(AssetEnum.OnlineSettingsMenu);
        }));
        backButton.onClick.AddListener(() => MenuManager.Instance.OnBackPressed());
    }

    public override GameObject GetFirstButton()
    {
        return helpButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.HelpAndOptionsMenuCanvas;
    }
}
