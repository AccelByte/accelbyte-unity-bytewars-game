// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchSessionItem : MonoBehaviour
{
    [SerializeField]
    private Image avatarImg;
    [SerializeField]
    private TextMeshProUGUI nameTxt;
    [SerializeField]
    private TextMeshProUGUI serverTypeTxt;
    [SerializeField]
    private TextMeshProUGUI matchTypeTxt;
    [SerializeField]
    private TextMeshProUGUI playerOccupancyTxt;
    [SerializeField]
    private Button joinBtn;
    private readonly Vector2 centerPivot = new Vector2(0.5f, 0.5f);
    private string matchSessionId;
    private BrowseMatchItemModel model;
    private Action<JoinMatchSessionRequest> onJoinMatchSession;

    public static Action<string> OnJoinButtonClicked = delegate { };
    public static Action<GameObject> OnJoinButtonDataSet;

    private void Start()
    {
        joinBtn.onClick.AddListener(ClickJoinBtn);
    }

    private void ClickJoinBtn()
    {
        onJoinMatchSession?
            .Invoke(new JoinMatchSessionRequest(matchSessionId, model.GameMode));

        OnJoinButtonClicked?.Invoke(matchSessionId);
    }

    public void SetData(BrowseMatchItemModel model, Action<JoinMatchSessionRequest> onJoinMatchSession)
    {
        this.model = model;
        this.model.OnDataUpdated = OnDataUpdated;
        this.onJoinMatchSession = onJoinMatchSession;
        SetView(model);
        gameObject.SetActive(true);

        OnJoinButtonDataSet?.Invoke(joinBtn.gameObject);
    }

    private void SetView(BrowseMatchItemModel model)
    {
        matchSessionId = model.MatchSessionId;
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
        model = updatedData;
        SetView(updatedData);
    }

    private string GetServerType(GameSessionServerType sessionServerType)
    {
        switch (sessionServerType)
        {
            case GameSessionServerType.DedicatedServer or GameSessionServerType.DedicatedServerAMS:
                return "DS";
            case GameSessionServerType.PeerToPeer:
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
            case InGameMode.CreateMatchTeamDeathmatch:
                return "Team Deathmatch";
            case InGameMode.CreateMatchElimination:
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
                if (avatarImg.gameObject.activeInHierarchy)
                    avatarImg.sprite =
                        Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), centerPivot);
            }
        });
    }
}
