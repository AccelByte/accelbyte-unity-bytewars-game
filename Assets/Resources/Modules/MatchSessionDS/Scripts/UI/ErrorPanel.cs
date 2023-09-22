using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ErrorPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI errorLabel;
    [SerializeField] private Button okButton;
    private UnityAction _onOkButtonClicked;
    void Start()
    {
        okButton.onClick.AddListener(_onOkButtonClicked);
    }

    public void Show(string errorInfo, UnityAction errorOkCallback)
    {
        errorLabel.text = errorInfo;
        _onOkButtonClicked = errorOkCallback;
        gameObject.SetActive(true);
    }
    
}
