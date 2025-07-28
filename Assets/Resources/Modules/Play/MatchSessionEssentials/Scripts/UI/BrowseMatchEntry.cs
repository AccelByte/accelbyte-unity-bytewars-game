// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MatchSessionEssentialsModels;

public class BrowseMatchEntry : MonoBehaviour
{
    public delegate void JoinMatchHandler(BrowseSessionModel sessionModel);

    public static event JoinMatchHandler OnJoinDSMatchButtonClicked = delegate { };
    public static event JoinMatchHandler OnJoinP2PMatchButtonClicked = delegate { };

    [SerializeField] private AccelByteWarsAsyncImage sessionOwnerImage;
    [SerializeField] private TMP_Text sessionOwnerText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private TMP_Text serverTypeText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private Button joinButton;

    public void Setup(BrowseSessionModel sessionModel)
    {
        string ownerName = 
            string.IsNullOrEmpty(sessionModel.Owner.UniqueDisplayName) ?
            string.IsNullOrEmpty(sessionModel.Owner.DisplayName) ? 
            $"Player-{sessionModel.Owner.UserId[..5]}" : 
            sessionModel.Owner.DisplayName : 
            sessionModel.Owner.UniqueDisplayName;
        sessionOwnerText.text = $"{ownerName}'s Session";
        sessionOwnerImage.LoadImage(sessionModel.Owner.AvatarUrl);

        memberCountText.text = $"{sessionModel.CurrentMemberCount}/{sessionModel.MaxMemberCount}";

        switch (sessionModel.GameMode)
        {
            case InGameMode.MatchmakingElimination:
            case InGameMode.CreateMatchElimination:
                gameModeText.text = "Elimination";
                break;
            case InGameMode.MatchmakingTeamDeathmatch:
            case InGameMode.CreateMatchTeamDeathmatch:
                gameModeText.text = "Team Deathmatch";
                break;
            default:
                gameModeText.text = "Single Player";
                break;
        }

        joinButton.gameObject.SetActive(sessionModel.ServerType != GameSessionServerType.None);
        joinButton.onClick.RemoveAllListeners();
        switch (sessionModel.ServerType)
        {
            case GameSessionServerType.DedicatedServer:
            case GameSessionServerType.DedicatedServerAMS:
                serverTypeText.text = "DS";
                joinButton.onClick.AddListener(() => OnJoinDSMatchButtonClicked?.Invoke(sessionModel));
                break;
            case GameSessionServerType.PeerToPeer:
                serverTypeText.text = "P2P";
                joinButton.onClick.AddListener(() => OnJoinP2PMatchButtonClicked?.Invoke(sessionModel));
                break;
            default:
                serverTypeText.text = "OFFLINE";
                break;
        }
    }
}
