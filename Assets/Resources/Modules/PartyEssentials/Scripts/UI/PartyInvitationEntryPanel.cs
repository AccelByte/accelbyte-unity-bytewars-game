using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PartyInvitationEntryPanel : MonoBehaviour
{
    [SerializeField] private Image senderImage;
    [SerializeField] private TMPro.TMP_Text invitationText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;

    private PartyEssentialsWrapper _partyWrapper;
    private PartyHelper _partyHelper;

    private string _partyId;
    
    // Start is called before the first frame update
    void Start()
    {
        _partyWrapper = TutorialModuleManager.Instance.GetModuleClass<PartyEssentialsWrapper>();
        _partyHelper = TutorialModuleManager.Instance.GetComponentInChildren<PartyHelper>();
        
        acceptButton.onClick.AddListener(AcceptPartyInvitation);
        rejectButton.onClick.AddListener(RejectPartyInvitation);
    }

    public void UpdatePartyInvitationInfo(string partyId, string senderName, Texture2D senderAvatar = null)
    {
        _partyId = partyId;
        invitationText.text = senderName + " invited you to join their Party";
        if (senderAvatar != null)
        {
            senderImage.sprite = Sprite.Create(senderAvatar, new Rect(0f, 0f, senderAvatar.width, senderAvatar.height), Vector2.zero);
        }
    }

    private void AcceptPartyInvitation()
    {
        if (_partyId != null)
        {
            _partyWrapper.JoinParty(_partyId, OnJoinPartyCompleted);
        }
    }

    private void RejectPartyInvitation()
    {
        if (_partyId != null)
        {
            _partyWrapper.RejectPartyInvitation(_partyId, OnRejectPartyInvitationCompleted);
        }
    }

    #region Callback Functions

    private void OnJoinPartyCompleted(Result<SessionV2PartySession> result)
    {
        _partyHelper.HandleJoiningParty(result.Value);
        Destroy(this.gameObject);
    }

    private void OnRejectPartyInvitationCompleted(Result result)
    {
        Destroy(this.gameObject);
    }

    #endregion
}