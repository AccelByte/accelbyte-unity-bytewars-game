using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IDeselectHandler
{
    public TMP_Text text;
    public Button button;
    [SerializeField] private Selectable selectable;
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    [SerializeField] private RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
      button.onClick.AddListener(OnClickAnimation);
    }

    private void Update()
    {
        text.color = button.IsInteractable() ? Color.white : Color.gray;
    }

    void OnClickAnimation()
    {
        var color = text.color;
        var fadeoutcolor = color;
        fadeoutcolor.a = 0;

        LeanTween.value(text.gameObject, UpdateValueExampleCallback,
            fadeoutcolor, color, 0.1f).setLoopPingPong(6).setOnComplete(OnComplete);
        AudioManager.Instance?.PlaySfx("Click_on_Button");
    }
    
    private void UpdateValueExampleCallback(Color val)
    {
        text.color = val;
    }
    private void OnComplete()
    {
        text.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selectable.OnPointerExit(null);
    }

    private void OnEnable()
    {
        if (contentSizeFitter != null
            && rectTransform!=null)
        {
            StartCoroutine(FixLayout());
        }
    }

    IEnumerator FixLayout()
    {
        yield return new WaitForEndOfFrame();
        var size = rectTransform.sizeDelta;
        yield return new WaitForEndOfFrame();
        contentSizeFitter.enabled = false;
        yield return new WaitForEndOfFrame();
        rectTransform.sizeDelta = size;
    }
}
