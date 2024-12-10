using System.Collections.Generic;
using TMPro;
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

    private void OnEnable()
    {
        // put your code here
        
        
        // On leaving party, set the leave button color to gray. Finished executing after reopening the party menu
        if (!leaveButton.interactable)
        {
            TMP_Text leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>();
            leaveButtonText.color = leaveButton.interactable ? Color.white : Color.gray;
        }
    }
    
    // put your code here
    
    public void SetLeaveButtonInteractable(bool isInteractable)
    {
        leaveButton.interactable = isInteractable;
        
        TMP_Text leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>();
        leaveButtonText.color = isInteractable ? Color.white : Color.gray;
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
