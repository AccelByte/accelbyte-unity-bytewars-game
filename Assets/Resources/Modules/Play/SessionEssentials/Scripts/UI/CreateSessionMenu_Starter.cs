// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SessionEssentialsModels;

public class CreateSessionMenu_Starter : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Transform createSessionPanel;
    [SerializeField] private Transform sessionResultPanel;
    [SerializeField] private TMP_Text sessionIdText;
    [SerializeField] private Button createSessionButton;
    [SerializeField] private Button leaveSessionButton;
    [SerializeField] private Button backButton;

    // TODO: Declare the tutorial module variables here.

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Add the tutorial module code here.
    }

    private void OnEnable()
    {
         // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.

    public override GameObject GetFirstButton()
    {
        return AccelByteWarsOnlineSession.CachedSession == null ? createSessionButton.gameObject : leaveSessionButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateSessionMenu_Starter;
    }
}
