// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaxActivePanel : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_InputField PlayerIdInput;
    public TMP_InputField SessionConfigurationInput;
    public Button TriggerButton;
    public Button CloseButton;    
    public RectTransform LogResult;
    private TMP_Text scrolViewText;


    // Start is called before the first frame update
    void Start()
    {
        CloseButton.onClick.AddListener(OnClosePanel);
        scrolViewText = LogResult.GetComponentInChildren<TMP_Text>();
    }

    private void OnDisable()
    {
        Title.text = string.Empty;
        TriggerButton.name = string.Empty;
        scrolViewText.color = Color.white;
        scrolViewText.text = string.Empty;

    }

    void OnClosePanel()
    {
        this.gameObject.SetActive(false);
    }

    public void SetTitle(string title)
    {
        Title.text = title;
    }
}
