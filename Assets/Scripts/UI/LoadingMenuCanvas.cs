using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingMenuCanvas : MenuCanvas
{
    [SerializeField] private Image[] loadingImages;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private GameObject timeoutContainer;
    [SerializeField] private TextMeshProUGUI timeoutInfo;

    private int _index = 0;

    private const float animationSpeed = 0.65f;
    
    // Start is called before the first frame update
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

    public override GameObject GetFirstButton()
    {
        if (cancelBtn.gameObject.activeSelf)
            return cancelBtn.gameObject;
        else
            return null;
    }

    private int loadingTimeoutSec = 0;
    private string timeoutPrefix;
    private string timeoutReachedInfo;
    public void Show(string loadingInfo, LoadingTimeoutInfo loadingTimeoutInfo=null, UnityAction cancelCallback=null)
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

        if (loadingTimeoutInfo == null)
        {
            timeoutContainer.gameObject.SetActive(false);
        }
        else
        {
            timeoutContainer.gameObject.SetActive(true);
            loadingTimeoutSec = loadingTimeoutInfo.timeoutSec;
            timeoutPrefix = loadingTimeoutInfo.info;
            timeoutReachedInfo = loadingTimeoutInfo.timeoutReachedError;
            UpdateTimeoutLabel();
            oneSecondCounter = 0;
        }
    }

    private void UpdateTimeoutLabel()
    {
        timeoutInfo.text = timeoutPrefix + loadingTimeoutSec;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.LoadingMenuCanvas;
    }
    private float oneSecondCounter = 0;
    private void FixedUpdate()
    {
        if(timeoutContainer!=null && timeoutContainer.activeSelf)
        {
            if(loadingTimeoutSec>0)
            {
                oneSecondCounter+=Time.fixedDeltaTime;
                if(oneSecondCounter>=1)
                {
                    oneSecondCounter=0;
                    loadingTimeoutSec--;
                    UpdateTimeoutLabel();
                }
            }
            else
            {
                MenuManager.Instance.ShowInfo(timeoutReachedInfo, "Timeout");
                gameObject.SetActive(false);
            }
        }
    }
}

public class LoadingTimeoutInfo
{
    public string info;
    public string timeoutReachedError;
    public int timeoutSec;
}