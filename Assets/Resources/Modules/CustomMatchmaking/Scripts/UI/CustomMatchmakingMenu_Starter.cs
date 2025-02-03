// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;
using static AccelByteWarsWidgetSwitcher;

public class CustomMatchmakingMenu_Starter : MenuCanvas
{
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private Button startMatchmakingButton;
    [SerializeField] private Button backButton;

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.CustomMatchmakingMenu_Starter;
    }

    public override GameObject GetFirstButton()
    {
        return startMatchmakingButton.gameObject;
    }

    // TODO: Add your code here.
}
