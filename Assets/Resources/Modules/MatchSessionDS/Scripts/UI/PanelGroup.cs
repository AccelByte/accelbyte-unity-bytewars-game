using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelGroup : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GameObject firstBtnToHighlight; 
    private const LeanTweenType leanTweenType = LeanTweenType.easeOutExpo;
    private const float TweenDuration = 0.8f;

    public RectTransform Show()
    {
        rectTransform.gameObject.SetActive(true);
        LeanTween.moveX(rectTransform, 0, TweenDuration).setEase(leanTweenType);
        EventSystem.current.SetSelectedGameObject(firstBtnToHighlight);
        return rectTransform;
    }

    public void HideSlideRight()
    {
        LeanTween.moveX(rectTransform, Screen.width, TweenDuration).setEase(leanTweenType)
            .setOnComplete(OnHideComplete);
    }
    public void HideRight()
    {
        rectTransform.gameObject.SetActive(false);
        rectTransform.anchoredPosition = new Vector2(Screen.width, 0);
    }
    public void HideSlideLeft()
    {
        LeanTween.moveX(rectTransform, -Screen.width, TweenDuration).setEase(leanTweenType)
            .setOnComplete(OnHideComplete);
    }

    private void OnHideComplete()
    {
        rectTransform.gameObject.SetActive(false);
    }
}
