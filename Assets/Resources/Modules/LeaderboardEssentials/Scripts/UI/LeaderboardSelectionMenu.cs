using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using Extensions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSelectionMenu : MenuCanvas
{
    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    private LeaderboardEssentialsWrapper leaderboardWrapper;

    public static string chosenLeaderboardCode;
    public static Dictionary<string, string[]> leaderboardCycleIds;

    private string leaderboardDataSerialized;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        leaderboardListPanel.DestroyAllChildren();

        leaderboardWrapper = TutorialModuleManager.Instance.GetModuleClass<LeaderboardEssentialsWrapper>();

        DisplayLeaderboardList();
    }

    private void OnEnable()
    {
        if (!leaderboardWrapper) 
        {
            return;
        }

        DisplayLeaderboardList();
    }

    private void ChangeToLeaderboardCycleMenu(string newLeaderboardCode)
    {
        chosenLeaderboardCode = newLeaderboardCode;
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardCycleMenuCanvas);
    }

    private void OnGetLeaderboardListCompleted(Result<LeaderboardPagedListV3> result)
    {
        if (result.IsError) 
        {
            return;
        }

        if (IsLeaderboardDataCached(result.Value.Data))
        {
            return;
        }
        
        leaderboardCycleIds = new Dictionary<string, string[]>();
        leaderboardListPanel.DestroyAllChildren();

        foreach (LeaderboardDataV3 leaderboardData in result.Value.Data)
        {
            if (!leaderboardData.Name.Contains("Unity")) continue;

            Button leaderboardButton =
                Instantiate(leaderboardItemButtonPrefab, leaderboardListPanel).GetComponent<Button>();
            TMP_Text leaderboardButtonText = leaderboardButton.GetComponentInChildren<TMP_Text>();
            leaderboardButtonText.text = leaderboardData.Name.Replace("Unity Leaderboard ", "");

            leaderboardButton.onClick.AddListener(() => ChangeToLeaderboardCycleMenu(leaderboardData.LeaderboardCode));

            leaderboardCycleIds.Add(leaderboardData.LeaderboardCode, leaderboardData.CycleIds);
        }
    }

    private void DisplayLeaderboardList()
    {
        leaderboardWrapper.GetLeaderboardList(OnGetLeaderboardListCompleted);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return backButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardSelectionMenuCanvas;
    }

    private bool IsLeaderboardDataCached(LeaderboardDataV3[] newData)
    {
        string newDataSerialized = JsonConvert.SerializeObject(newData);
        
        bool isCached = leaderboardDataSerialized != null && leaderboardDataSerialized == newDataSerialized;
        if (isCached)
        {
            return true;
        }
        
        leaderboardDataSerialized = newDataSerialized;
        return false;
    }
}