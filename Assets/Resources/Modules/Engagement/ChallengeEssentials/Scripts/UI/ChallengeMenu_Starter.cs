// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChallengeEssentialsModels;

public class ChallengeMenu_Starter : MenuCanvas
{
    [SerializeField] private ChallengeEntry_Starter challengeEntryPrefab;
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private TMP_Text challengeTitleText;
    [SerializeField] private Transform challengeListPanel;
    [SerializeField] private Button backButton;

    // TODO: Declare tutorial module variables here.

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.ChallengeMenu_Starter;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
