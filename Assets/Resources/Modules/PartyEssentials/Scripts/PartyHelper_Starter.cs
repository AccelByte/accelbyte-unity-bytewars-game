using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyHelper_Starter : MonoBehaviour
{
    private GameObject _partyInvitationPrefab;
    private GameObject _messageNotificationPrefab;
    
    // put your code here
    

    // Start is called before the first frame update
    void Start()
    {
        _partyInvitationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.PartyInvitationEntryPanel) as GameObject;
        _messageNotificationPrefab = AssetManager.Singleton.GetAsset(AssetEnum.MessageNotificationEntryPanel) as GameObject;

        LoginHandler.onLoginCompleted += data =>
        {
            // put your code here
            
        };
    }

    // put your code here
    
}