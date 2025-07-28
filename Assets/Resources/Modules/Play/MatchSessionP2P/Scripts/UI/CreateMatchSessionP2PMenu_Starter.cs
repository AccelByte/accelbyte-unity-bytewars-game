// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using static SessionEssentialsModels;
using static MatchSessionEssentialsModels;

public class CreateMatchSessionP2PMenu_Starter : MenuCanvas
{
    [SerializeField] private MatchSessionStateSwitcher stateSwitcher;

    // TODO: Declare the tutorial module variables here.

    private void Awake()
    {
        // TODO: Add the tutorial module code here.
    }

    private void OnEnable()
    {
        // TODO: Add the tutorial module code here.
    }

    private void OnDisable()
    {
        // TODO: Add the tutorial module code here.
    }

    // TODO: Declare the tutorial module functions here.

    public override GameObject GetFirstButton()
    {
        return stateSwitcher.DesiredFocus;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CreateMatchSessionP2PMenu_Starter;
    }
}
