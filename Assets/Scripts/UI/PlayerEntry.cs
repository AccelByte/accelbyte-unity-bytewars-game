// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntry : MonoBehaviour
{
    [SerializeField] private Image playerAvatar;
    [SerializeField] private TextMeshProUGUI playerName;
    
    private string avatarURL;
    private readonly Vector2 centerPivot = new Vector2(0.5f, 0.5f);

    public void Set(TeamState teamState, PlayerState playerState, bool isCurrentPlayer)
    {
        string playerName = playerState.GetPlayerName();
        this.playerName.text = isCurrentPlayer ? $"{playerName} (You)" : playerName;

        avatarURL = playerState.avatarUrl;

        this.playerName.color = teamState.teamColour;
        playerAvatar.color = teamState.teamColour;
    }
    
    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(avatarURL))
        {
            Vector2 sizeDelta = playerAvatar.rectTransform.sizeDelta;
            int imageWidth = (int)sizeDelta.x;
            int imageHeight = (int)sizeDelta.y;

            CacheHelper.LoadTexture(avatarURL, imageWidth, imageHeight, texture =>
            {
                if (texture != null)
                {
                    playerAvatar.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), centerPivot);
                    playerAvatar.color = Color.white;
                }
            });
        }
    }
}