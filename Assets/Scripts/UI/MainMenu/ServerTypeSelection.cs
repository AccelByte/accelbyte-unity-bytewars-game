// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class ServerTypeSelection : MenuCanvas
{
    [SerializeField] private Button dsButton;
    [SerializeField] private Button p2pButton;
    [SerializeField] private Button backButton;
    void Start()
    {
        dsButton.onClick.AddListener(ClickDsButton);
        p2pButton.onClick.AddListener(ClickP2PButton);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void ClickDsButton()
    {
        GameData.ServerType = ServerType.OnlineDedicatedServer;
        MenuManager.Instance.ChangeToMenu(AssetEnum.QuickPlayGameMenu);
    }

    private void ClickP2PButton()
    {
        GameData.ServerType = ServerType.OnlinePeer2Peer;
        MenuManager.Instance.ChangeToMenu(AssetEnum.QuickPlayGameMenu);
    }
    public override GameObject GetFirstButton()
    {
        return dsButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.ServerTypeSelection;
    }
}
