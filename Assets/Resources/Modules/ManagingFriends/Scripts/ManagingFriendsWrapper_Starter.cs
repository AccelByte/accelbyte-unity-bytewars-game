using System.Collections;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class ManagingFriendsWrapper_Starter : MonoBehaviour
{
    private Lobby _lobby;

    // Start is called before the first frame update
    void Start()
    {
        _lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
    }


}
