using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler : MenuCanvas
{
    [SerializeField] private Transform[] partyMemberEntryPanels;
    [SerializeField] private Button leavePartyButton;
    [SerializeField] private Button backButton;
    
    // Start is called before the first frame update
    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }
    
    public override GameObject GetFirstButton()
    {
        return leavePartyButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenuCanvas;
    }
}
