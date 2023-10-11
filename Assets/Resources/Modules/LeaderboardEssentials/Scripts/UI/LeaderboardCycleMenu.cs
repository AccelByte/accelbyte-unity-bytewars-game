using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardCycleMenu : MenuCanvas
{
    [SerializeField] private Button allTimeButton;
    [SerializeField] private Button backButton;

    [SerializeField] private Transform leaderboardListPanel;
    [SerializeField] private GameObject leaderboardItemButtonPrefab;

    public static LeaderboardCycleType chosenCycleType;

    public enum LeaderboardCycleType
    {
        AllTime,
        Weekly
    }

    public delegate void LeaderboardCycleMenuDelegate(Transform leaderboardListPanel,
        GameObject leaderboardItemButtonPrefab);

    public static event LeaderboardCycleMenuDelegate onLeaderboardCycleMenuActivated = delegate { };

    private void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);

        allTimeButton.onClick.AddListener(() => ChangeToLeaderboardMenu(LeaderboardCycleType.AllTime));

        onLeaderboardCycleMenuActivated.Invoke(leaderboardListPanel, leaderboardItemButtonPrefab);
    }

    public static void ChangeToLeaderboardMenu(LeaderboardCycleType cycleType)
    {
        chosenCycleType = cycleType;
        MenuManager.Instance.ChangeToMenu(AssetEnum.LeaderboardMenuCanvas);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return allTimeButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LeaderboardCycleMenuCanvas;
    }
}