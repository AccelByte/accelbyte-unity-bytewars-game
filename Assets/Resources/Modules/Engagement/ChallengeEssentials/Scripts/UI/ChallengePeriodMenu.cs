// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;
using UnityEngine.UI;
using static ChallengeEssentialsModels;

public class ChallengePeriodMenu : MenuCanvas
{
    public static ChallengeRotation SelectedPeriod { get; private set; }

    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button dailyButton;
    [SerializeField] private Button weeklyButton;
    [SerializeField] private Button backButton;

    private ModuleModel challengeEssentials;

    private void Awake()
    {
        allTimeButton.onClick.AddListener(() => ChangeToChallengeMenu(ChallengeRotation.None));
        dailyButton.onClick.AddListener(() => ChangeToChallengeMenu(ChallengeRotation.Daily));
        weeklyButton.onClick.AddListener(() => ChangeToChallengeMenu(ChallengeRotation.Weekly));
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    private void ChangeToChallengeMenu(ChallengeRotation selectedPeriod)
    {
        SelectedPeriod = selectedPeriod;

        challengeEssentials ??= TutorialModuleManager.Instance.GetModule(TutorialType.ChallengeEssentials);
        MenuManager.Instance.ChangeToMenu(challengeEssentials.isStarterActive ? AssetEnum.ChallengeMenu_Starter : AssetEnum.ChallengeMenu);
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.ChallengePeriodMenu;
    }

    public override GameObject GetFirstButton()
    {
        return allTimeButton.gameObject;
    }
}
