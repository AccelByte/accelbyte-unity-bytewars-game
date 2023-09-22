using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchSessionItem : MonoBehaviour
{
    [SerializeField] private Image avatarImg;
    [SerializeField] private TextMeshProUGUI nameTxt;
    [SerializeField] private TextMeshProUGUI serverTypeTxt;
    [SerializeField] private TextMeshProUGUI matchTypeTxt;
    [SerializeField] private TextMeshProUGUI playerOccupancyTxt;
    [SerializeField] private Button joinBtn;
    private readonly Vector2 _centerPivot = new Vector2(0.5f, 0.5f);
    private const string ClassName = "[MatchSessionItem]";
    private string _matchSessionId;
    private BrowseMatchItemModel _model;
    private Action<JoinMatchSessionRequest> _onJoinMatchSession;
    void Start()
    {
        joinBtn.onClick.AddListener(ClickJoinBtn);
    }

    private void ClickJoinBtn()
    {
        _onJoinMatchSession?
            .Invoke(new JoinMatchSessionRequest(_matchSessionId, _model.GameMode));
    }

    public void SetData(BrowseMatchItemModel model, Action<JoinMatchSessionRequest> onJoinMatchSession)
    {
        _model = model;
        _model.OnDataUpdated = OnDataUpdated;
        _onJoinMatchSession = onJoinMatchSession;
        SetView(model);
        gameObject.SetActive(true);
    }

    private void SetView(BrowseMatchItemModel model)
    {
        _matchSessionId = model.MatchSessionId;
        nameTxt.text = model.MatchCreatorName;
        if (!String.IsNullOrEmpty(model.MatchCreatorAvatarURL))
        {
            SetAvatar(model.MatchCreatorAvatarURL);
        }
        serverTypeTxt.text = GetServerType(model.SessionServerType);
        matchTypeTxt.text = GetMatchType(model.GameMode);
        playerOccupancyTxt.text = GetPlayerOccupancyLabel(model);
    }

    private void OnDataUpdated(BrowseMatchItemModel updatedData)
    {
        _model = updatedData;
        SetView(updatedData);
    }

    private string GetServerType(MatchSessionServerType sessionServerType)
    {
        switch (sessionServerType)
        {
            case MatchSessionServerType.DedicatedServer:
                return "DS";
            case MatchSessionServerType.PeerToPeer:
                return "P2P";
        }

        return "N/A";
    }

    private string GetPlayerOccupancyLabel(BrowseMatchItemModel model)
    {
        joinBtn.interactable = model.CurrentPlayerCount != model.MaxPlayerCount;
        return $"{model.CurrentPlayerCount}/{model.MaxPlayerCount} Players";
    }

    private string GetMatchType(InGameMode gameMode)
    {
        switch (gameMode)
        {
            case InGameMode.CreateMatchDeathMatchGameMode:
                return "Team Deathmatch";
            case InGameMode.CreateMatchEliminationGameMode:
                return "Elimination";
        }
        return gameMode.ToString();
    }

    private void SetAvatar(string avatarUrl)
    {
        if (!avatarImg.rectTransform)
            return;
        var sizeDelta = avatarImg.rectTransform.sizeDelta;
        var imageWidth = (int)sizeDelta.x;
        var imageHeight = (int)sizeDelta.y;
        CacheHelper.LoadTexture(avatarUrl, imageWidth, imageHeight, texture =>
        {
            if (texture != null)
            {
                if(avatarImg.gameObject.activeInHierarchy)
                    avatarImg.sprite = 
                        Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), _centerPivot);
            }
        });
    }
}
