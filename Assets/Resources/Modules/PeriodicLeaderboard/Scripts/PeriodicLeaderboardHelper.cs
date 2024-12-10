// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PeriodicLeaderboardHelper : MonoBehaviour
{
    private PeriodicLeaderboardEssentialsWrapper periodicLeaderboardWrapper;
    private const int RESULTOFFSET = 0;
    private const int RESULTLIMIT = 10;
    
    void Start()
    {
        periodicLeaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<PeriodicLeaderboardEssentialsWrapper>();
        
        LeaderboardCycleMenu.onLeaderboardCycleMenuActivated += DisplayLeaderboardCycleButtons;
        LeaderboardMenu.onDisplayRankingListEvent += DisplayCycleRankingList;
    }
    
    private void DisplayLeaderboardCycleButtons(LeaderboardCycleMenu leaderboardCycleMenu, Transform leaderboardListPanel, GameObject leaderboardItemButtonPrefab)
    {
        string[] cycleIds = LeaderboardSelectionMenu.leaderboardCycleIds[LeaderboardSelectionMenu.chosenLeaderboardCode];
        
        // No valid leaderboard cycles.
        if (cycleIds.Length <= 0)
        {
            BytewarsLogger.LogWarning("Cannot display leaderboard cycle buttons. No leaderboard cycle id was found.");
            return;
        }

        // Query leaderboard cycles.
        int queriedCycles = 0;
        leaderboardCycleMenu.CurrentView = LeaderboardCycleMenu.LeaderboardCycleView.Loading;
        foreach (string cycleId in cycleIds)
        {
            periodicLeaderboardWrapper.GetStatCycleConfig(cycleId, result =>
            {
                if (!result.IsError)
                {
                    Button leaderboardButton = Instantiate(leaderboardItemButtonPrefab, leaderboardListPanel).GetComponent<Button>();
                    TMP_Text leaderboardButtonText = leaderboardButton.GetComponentInChildren<TMP_Text>();
                    leaderboardButtonText.text = result.Value.Name.Replace("unity-", "");
                    leaderboardButton.onClick.AddListener(() => LeaderboardCycleMenu.ChangeToLeaderboardMenu(LeaderboardCycleMenu.LeaderboardCycleType.Weekly, cycleId));

                    queriedCycles++;

                    // Show the leaderboard cycle list if all cycles are already queried.
                    if (queriedCycles == cycleIds.Length)
                    {
                        BytewarsLogger.Log("Success to display leaderboard cycle buttons.");
                        leaderboardCycleMenu.CurrentView = LeaderboardCycleMenu.LeaderboardCycleView.Default;
                    }
                }
                else
                {
                    BytewarsLogger.LogWarning($"Failed to display leaderboard cycle buttons. Error: {result.Error.Message}");
                    leaderboardCycleMenu.CurrentView = LeaderboardCycleMenu.LeaderboardCycleView.Failed;
                }
            });
        }
    }

    private void DisplayCycleRankingList(LeaderboardMenu leaderboardMenu, UserCycleRanking[] userCycleRankings)
    {
        if (LeaderboardCycleMenu.chosenCycleType == LeaderboardCycleMenu.LeaderboardCycleType.AllTime)
        {
            BytewarsLogger.LogWarning("Cannot display leaderboard cycle ranking list. Chosen cycle type is not a periodic leaderboard.");
            return;
        }

        periodicLeaderboardWrapper.GetRankingsByCycle(
            LeaderboardSelectionMenu.chosenLeaderboardCode,
            LeaderboardCycleMenu.chosenCycleId,
            leaderboardMenu.OnGetRankingsCompleted,
            RESULTOFFSET,
            RESULTLIMIT);
    }
}
