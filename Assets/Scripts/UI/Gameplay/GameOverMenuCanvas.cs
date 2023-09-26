using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverMenuCanvas : MenuCanvas
{
    [SerializeField] 
    private TextMeshProUGUI winningPlayerTextUI;
    [SerializeField] 
    private LeaderboardEntryController[] leaderBoards;
    [SerializeField]
    private Button playAgainBtn;
    [SerializeField] 
    private Button quitBtn;

    [SerializeField] private RectTransform countdownContainer;
    [SerializeField] private TextMeshProUGUI countdownTxt;

    private void Start()
    {
        playAgainBtn.onClick.AddListener(OnPlayAgainButtonClicked);
        quitBtn.onClick.AddListener(OnQuitButtonClicked);
    }

    private void OnPlayAgainButtonClicked()
    {
        GameManager.Instance.RestartLocalGame();
    }

    private void OnQuitButtonClicked()
    {
        StartCoroutine(GameManager.Instance.QuitToMainMenu());
    }

    private void SetData(List<PlayerState> playerStates, TeamState[] teamStates)
    {
        playerStates.Sort((a, b) =>
        {
            return b.score.CompareTo(a.score);
        });
        winningPlayerTextUI.text = playerStates[0].GetPlayerName();
        winningPlayerTextUI.color = teamStates[playerStates[0].teamIndex].teamColour;
        for (int i = 0; i < playerStates.Count; i++)
        {
            var pState = playerStates[i];
            leaderBoards[i].SetDetails(pState.GetPlayerName(), teamStates[pState.teamIndex].teamColour, 
                pState.killCount, (int)pState.score);
            leaderBoards[i].gameObject.SetActive(true);
        }
    }
    public override GameObject GetFirstButton()
    {
        return playAgainBtn.gameObject;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.InGameState==InGameState.GameOver)
        {
            List<PlayerState> playerStates = GameManager.Instance.ConnectedPlayerStates.Values.ToList();
            SetData(playerStates, GameManager.Instance.TeamStates);
        }
        playAgainBtn.gameObject.SetActive(!NetworkManager.Singleton.IsListening); 
    }

    private void OnDisable()
    {
        foreach (var le in leaderBoards)
        {
            le.gameObject.SetActive(false);
        }
    }

    public void Countdown(int countdownSecond)
    {
        if(!countdownContainer.gameObject.activeSelf)
            countdownContainer.gameObject.SetActive(true);
        countdownTxt.text = "Game Over, Server will be shutdown in: " + countdownSecond;
        if (countdownSecond <= 0)
        {
            countdownContainer.gameObject.SetActive(false);
        }
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.GameOverMenuCanvas;
    }
}
