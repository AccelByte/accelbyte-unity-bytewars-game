// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;

public class TeamEntry : MonoBehaviour
{
    [SerializeField] private Image outline;
    [SerializeField] private RectTransform playerEntryContainer;

    public RectTransform PlayerEntryContainer
    {
        get { return playerEntryContainer; }
    }

    public void Set(TeamState teamState)
    {
        outline.color = teamState.teamColour;
    }
}
