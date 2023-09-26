using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntry : MonoBehaviour
{
    [SerializeField] private Image shipAvatar;

    [SerializeField] private TextMeshProUGUI shipName;
    [SerializeField] private Image playerAvatar;
    private string avatarUrl;

    public void Set(TeamState teamState, PlayerState playerState, bool isCurrentPlayer)
    {
        shipAvatar.color = teamState.teamColour;
        shipName.color = teamState.teamColour;
        var playerName = playerState.GetPlayerName();
        if (isCurrentPlayer)
        {
            shipName.text = $"You: {playerName}";
        }
        else
        {
            shipName.text = playerName;
        }

        avatarUrl = playerState.avatarUrl;
    }
    private readonly Vector2 _centerPivot = new Vector2(0.5f, 0.5f);
    private void OnEnable()
    {
        if (!String.IsNullOrEmpty(avatarUrl))
        {
            var sizeDelta = playerAvatar.rectTransform.sizeDelta;
            var imageWidth = (int)sizeDelta.x;
            var imageHeight = (int)sizeDelta.y;
            CacheHelper.LoadTexture(avatarUrl, imageWidth, imageHeight, texture =>
            {
                if(texture!=null)
                    playerAvatar.sprite = 
                        Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), _centerPivot);
            });
        }
    }
}