using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class PartyHelper : MonoBehaviour
{
    [SerializeField] private GameObject partyInvitationPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        LoginHandler.onLoginCompleted += data =>
        {
            Lobby lobby = MultiRegistry.GetApiClient().GetLobby();
            if (!lobby.IsConnected) lobby.Connect();
            
            lobby.SessionV2InvitedUserToParty += PartyHandler.ReceivePartyInvitation;
        };
    }
}