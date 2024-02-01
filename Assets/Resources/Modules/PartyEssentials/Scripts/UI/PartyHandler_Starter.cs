using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyHandler_Starter : MenuCanvas
{
    [SerializeField] private PartyMemberEntryPanel[] partyMemberEntryPanels;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button backButton;

    // put your code here
    
    
    void Start()
    {
        backButton.onClick.AddListener(OnBackButtonClicked);
    }
    
    // put your code here
    
    
    public void SetLeaveButtonInteractable(bool isInteractable)
    {
        leaveButton.interactable = isInteractable;
    }
    
    // put your code here
    

    private void OnBackButtonClicked()
    {
        MenuManager.Instance.OnBackPressed();
    }

    public override GameObject GetFirstButton()
    {
        return leaveButton.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.PartyMenuCanvas;
    }
}
