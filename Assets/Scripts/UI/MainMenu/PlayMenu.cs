using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayMenu : MenuCanvas
{
    // Start is called before the first frame update
    public Button backButton;
    public Button singlePlayerButton;
    public Button multiplayerButton;
    [SerializeField] private GameModeSO singlePlayerGameMode;
    
    void Start()
    {
        singlePlayerButton.onClick.AddListener(OnSinglePlayerButtonPressed);
        backButton.onClick.AddListener(MenuManager.Instance.OnBackPressed);
        multiplayerButton.onClick.AddListener(ClickMultiplayerButton);
    }

    private void ClickMultiplayerButton()
    {
        MenuManager.Instance.ChangeToMenu(AssetEnum.MultiplayerMenuCanvas);
    }
    
    public void OnSinglePlayerButtonPressed()
    {
        //GameManager.Instance.StartHost(InGameMode.SinglePlayerGameMode);
        GameManager.Instance.StartGame(singlePlayerGameMode);
    }

    private void OnHideAnimateComplete()
    {
        GameDirector.Instance.StartSinglePlayer();
    }

    public override GameObject GetFirstButton()
    {
        return singlePlayerButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PlayMenuCanvas;
    }
}
