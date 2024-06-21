// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsMenu : MonoBehaviour
{
    [Serializable]
    private class Tab
    {
        public string TabLabel;
        public RectTransform TabContent;
    }

    [SerializeField]
    private ButtonAnimation buttonPrefab;

    [SerializeField]
    private List<Tab> tabs = new List<Tab>();

    private void Start()
    {
        SetupTabButtons();
    }

    private void OnEnable()
    {
        // Show the first tab content.
        ShowTabContent(tabs[0]);
    }

    private void Reset()
    {
        transform.DestroyAllChildren();
    }

    private void SetupTabButtons()
    {
        Reset();

        if (tabs.Count < 0)
        {
            BytewarsLogger.LogWarning($"No tab was assigned. Tab menu is empty.");
            return;
        }

        // Generate tab buttons.
        foreach(Tab tab in tabs)
        {
            ButtonAnimation tabButton = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, transform);
            tabButton.text.text = tab.TabLabel;
            tabButton.button.onClick.AddListener(() => ShowTabContent(tab));
        }

        // Show the first tab content.
        ShowTabContent(tabs[0]);
    }

    private void ShowTabContent(Tab targetTab)
    {
        if (targetTab == null)
        {
            BytewarsLogger.LogWarning("Cannot show tab content. Tab is not valid.");
            return;
        }

        // Show the target tab.
        foreach (Tab tab in tabs)
        {
            tab.TabContent.gameObject.SetActive(false);
        }
        targetTab.TabContent.gameObject.SetActive(true);

        // Rebuild layout.
        if (transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetTab.TabContent);
    }
}
