// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;

public class PartyMenu_Starter : MenuCanvas
{
    [SerializeField] private Transform memberEntryContainer;
    [SerializeField] private PartyMemberEntry memberEntryPrefab;

    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;

    private PartyEssentialsWrapper_Starter partyWrapper;

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenu_Starter;
    }

    public override GameObject GetFirstButton()
    {
        return leaveButton.gameObject;
    }

    private void OnEnable()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);

        // TODO: Bind your party delegates.
    }

    private void OnDisable()
    {
        leaveButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();

        // TODO: Unbind your party delegates.
    }
}
