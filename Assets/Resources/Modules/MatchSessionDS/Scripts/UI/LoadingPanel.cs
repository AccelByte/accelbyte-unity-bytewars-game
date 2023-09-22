using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    [SerializeField] private Image[] loadingImages;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private TextMeshProUGUI infoText;
    private int _index = 0;
    private const float animationSpeed = 0.65f;
    void Start()
    {
        StartAnimate();
    }

    private void StartAnimate()
    {
        _index = 0;
        for (int i = 0; i < loadingImages.Length; i++)
        {
            loadingImages[i].color = new Color(1, 1, 1, 0);
            float tempI = i;
            Invoke("AnimateImage", tempI*animationSpeed);
        }
    }

    private void AnimateImage()
    {
        ;
        if (_index == loadingImages.Length - 1)
        {
            LeanTween.color(loadingImages[_index].rectTransform, Color.white, animationSpeed)
                .setLoopCount(2).setLoopType(LeanTweenType.pingPong)
                .setEaseOutQuad()
                .setOnComplete(StartAnimate);
        }
        else
        {
            LeanTween.color(loadingImages[_index].rectTransform, Color.white, animationSpeed)
                .setEaseOutQuad()
                .setLoopCount(2).setLoopType(LeanTweenType.pingPong);
            _index++;
        }
    }
    public void Show(string loadingInfo, UnityAction cancelCallback=null)
    {
        infoText.text = loadingInfo;
        if (cancelCallback!=null)
        {
            cancelBtn.gameObject.SetActive(true);
            cancelBtn.onClick.AddListener(cancelCallback);
        }
        else
        {
            cancelBtn.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
    }
}
