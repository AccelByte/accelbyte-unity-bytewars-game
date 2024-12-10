using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageNotificationEntryPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    public void ChangeMessageText(string message)
    {
        messageText.text = message;
    }
}
