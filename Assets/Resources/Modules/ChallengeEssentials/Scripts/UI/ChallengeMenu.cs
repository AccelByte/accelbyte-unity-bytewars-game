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

public class ChallengeMenu : MenuCanvas
{
    [SerializeField] private ChallengeEntry challengeEntryPrefab;
    [SerializeField] private AccelByteWarsWidgetSwitcher widgetSwitcher;
    [SerializeField] private TMP_Text challengeTitleText;
    [SerializeField] private Transform challengeListPanel;
    [SerializeField] private Button backButton;

    private ChallengeEssentialsWrapper challengeWrapper;

    private void Awake()
    {
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void OnEnable()
    {
        // Set menu title based on selected challenge period.
        challengeTitleText.text = 
            ChallengePeriodMenu.SelectedPeriod == ChallengeRotation.None ? 
            AllTimeChallengeTitleLabel : 
            string.Format(PeriodicChallengeTitleLabel, ChallengePeriodMenu.SelectedPeriod.ToString());

        // Get and display challenge goal list.
        challengeWrapper ??= TutorialModuleManager.Instance.GetModuleClass<ChallengeEssentialsWrapper>();
        if (challengeWrapper)
        {
            GetChallengeGoalList();
        }
    }

    private void GetChallengeGoalList()
    {
        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Loading);

        // Get challenge by period.
        challengeWrapper.GetChallengeByPeriod(
            ChallengePeriodMenu.SelectedPeriod,
            (Result<ChallengeResponseInfo> infoResult) =>
            {
                if (infoResult.IsError)
                {
                    widgetSwitcher.ErrorMessage = infoResult.Error.Message;
                    widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                    return;
                }

                // Get and display challenge goal list.
                int rotationIndex = 0; // Zero indicates the current and latest active challenge rotation.
                challengeWrapper.GetChallengeGoalList(
                    infoResult.Value,
                    (Result<List<ChallengeGoalData>> goalsResult) =>
                    {
                        if (goalsResult.IsError)
                        {
                            widgetSwitcher.ErrorMessage = goalsResult.Error.Message;
                            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Error);
                            return;
                        }

                        if (goalsResult.Value.Count <= 0)
                        {
                            widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Empty);
                            return;
                        }

                        // Display challenge goal entries.
                        challengeListPanel.DestroyAllChildren();
                        foreach(ChallengeGoalData goal in goalsResult.Value)
                        {
                            Instantiate(challengeEntryPrefab, challengeListPanel).Setup(goal);
                        }
                        widgetSwitcher.SetWidgetState(AccelByteWarsWidgetSwitcher.WidgetState.Not_Empty);
                    },
                    rotationIndex);
            });
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.ChallengeMenu;
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }
}
