using UnityEngine;
using UnityEngine.UI;


public class QuickPlayGameMenu : MenuCanvas
{
    public Button backButton;
    public Button eliminationButton;
    public Button teamDeadmatchButton;
    private readonly IMatchmaking matchmaking = new MatchmakingWrapper();
    void Start()
    {
        eliminationButton.onClick.AddListener(OnEliminationButtonPressed);
        teamDeadmatchButton.onClick.AddListener(OnTeamDeathMatchButtonPressed);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
    }

    public void OnEliminationButtonPressed()
    {
        MenuManager.Instance.ShowLoading("Finding Elimination Match...", null, ClickCancelMatchmakingElimination);
        //call dummy Accelbyte Game Services for matchmaking to get server ip address and port
        matchmaking.StartMatchmaking(InGameMode.OnlineEliminationGameMode, OnMatchmakingFinished);

    }

    private readonly LoadingTimeoutInfo loadingTimeoutInfo = new LoadingTimeoutInfo()
    {
        Info = "Joining match will timeout in: ",
        TimeoutReachedError = "Timeout in joining match",
        TimeoutSec = GameConstant.MaxConnectAttemptsSec
    };
    private void OnMatchmakingFinished(MatchmakingResult result)
    {
        if (result.m_isSuccess)
        {
            MenuManager.Instance.ShowLoading("JOINING MATCH", loadingTimeoutInfo);
            if (GameData.ServerType == ServerType.OnlineDedicatedServer)
            {
                Debug.Log("joining dedicated server...");
                GameManager.Instance
                    .StartAsClient(result.m_serverIpAddress, result.m_serverPort,
                        result.InGameMode);
            }
            else if (GameData.ServerType == ServerType.OnlinePeer2Peer)
            {
                if (result.isStartAsHostP2P)
                {
                    Debug.Log($"starting as p2p host ip{result.m_serverIpAddress} port:{result.m_serverPort} InGameMode:{result.InGameMode}");
                    string testServerSessionId = "test-server-session";
                    GameData.ServerSessionID = testServerSessionId;
                    GameManager.Instance
                        .StartAsHost("127.0.0.1", result.m_serverPort,
                            result.InGameMode, testServerSessionId);
                }
                else
                {
                    Debug.Log($"joining p2p server ip{result.m_serverIpAddress} port:{result.m_serverPort} InGameMode:{result.InGameMode}");
                    GameManager.Instance
                        .StartAsClient(result.m_serverIpAddress, result.m_serverPort,
                            result.InGameMode);
                }
            }

        }
        else
        {
            MenuManager.Instance.HideLoading();
            MenuManager.Instance.ShowInfo(result.m_errorMessage, "Error");
            Debug.Log("failed to matchmaking, please try again, error: " + result.m_errorMessage);
        }
    }


    private void ClickCancelMatchmakingElimination()
    {
        //TODO cancel matchmaking using SDK too
        matchmaking.CancelMatchmaking();
        MenuManager.Instance.HideLoading();
    }

    private void OnTeamDeathMatchButtonPressed()
    {
        MenuManager.Instance.ShowLoading("Finding Team Death-match ...", null, ClickCancelMatchmakingElimination);
        //call dummy Accelbyte Game Services for matchmaking to get server ip address and port
        matchmaking.StartMatchmaking(InGameMode.OnlineDeathMatchGameMode,
            OnMatchmakingFinished);
    }

    public override GameObject GetFirstButton()
    {
        return eliminationButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.QuickPlayGameMenu;
    }
}
