using System.Collections;
using System.Collections.Generic;
using AccelByte.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PeriodicLeaderboardHelper : MonoBehaviour
{
    private PeriodicLeaderboardEssentialsWrapper periodicLeaderboardWrapper;
    private AuthEssentialsWrapper authWrapper;
    
    private TokenData currentUserData; 
    private string chosenCycleId;
    private const int RESULTOFFSET = 0;
    private const int RESULTLIMIT = 10;
    
    void Start()
    {
        periodicLeaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<PeriodicLeaderboardEssentialsWrapper>();
        authWrapper = TutorialModuleManager.Instance.GetModuleClass<AuthEssentialsWrapper>();
        currentUserData = authWrapper.userData;
        
        LeaderboardCycleMenu.onLeaderboardCycleMenuActivated += DisplayLeaderboardCycleButtons;
        LeaderboardMenu.onDisplayRankingListEvent += DisplayCycleRankingList;
        LeaderboardMenu.onDisplayUserRankingEvent += DisplayUserCycleRanking;
    }
    
    private void DisplayLeaderboardCycleButtons(Transform leaderboardListPanel, GameObject leaderboardItemButtonPrefab)
    {
        string[] cycleIds = LeaderboardSelectionMenu.leaderboardCycleIds[LeaderboardSelectionMenu.chosenLeaderboardCode];

        foreach (string cycleId in cycleIds)
        {
            periodicLeaderboardWrapper.GetStatCycleConfig(cycleId, result =>
            {
                if (!result.IsError)
                {
                    Button leaderboardButton = Instantiate(leaderboardItemButtonPrefab, leaderboardListPanel).GetComponent<Button>();
                    TMP_Text leaderboardButtonText = leaderboardButton.GetComponentInChildren<TMP_Text>();
                    leaderboardButtonText.text = result.Value.Name;
                    leaderboardButton.onClick.AddListener(() => LeaderboardCycleMenu.ChangeToLeaderboardMenu(LeaderboardCycleMenu.LeaderboardCycleType.Weekly));

                    chosenCycleId = cycleId;
                }
            });
        }
    }

    private void DisplayCycleRankingList(LeaderboardMenu leaderboardMenu, UserCycleRanking[] userCycleRankings)
    {
        if (LeaderboardCycleMenu.chosenCycleType is LeaderboardCycleMenu.LeaderboardCycleType.Weekly)
        {
            periodicLeaderboardWrapper.GetRankingsByCycle(LeaderboardSelectionMenu.chosenLeaderboardCode, chosenCycleId, leaderboardMenu.OnGetRankingsCompleted, RESULTOFFSET, RESULTLIMIT);
        }
    }

    private void DisplayUserCycleRanking(LeaderboardMenu leaderboardMenu, UserCycleRanking[] userCycleRankings)
    {
        if (LeaderboardCycleMenu.chosenCycleType is LeaderboardCycleMenu.LeaderboardCycleType.Weekly)
        {
            foreach (UserCycleRanking cycleRanking in userCycleRankings)
            {
                if (cycleRanking.CycleId == chosenCycleId)
                {
                    leaderboardMenu.InstantiateRankingEntry(currentUserData.user_id, cycleRanking.Rank, currentUserData.display_name, cycleRanking.Point);
                    break;
                }
            }
        }
    }
}
