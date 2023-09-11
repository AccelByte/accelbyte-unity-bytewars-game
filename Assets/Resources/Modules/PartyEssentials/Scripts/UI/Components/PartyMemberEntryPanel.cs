using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberEntryPanel : MonoBehaviour
{
    public enum PartyEntryView
    {
        Empty,
        MemberInfo
    }
    private PartyEntryView _displayEntryView = PartyEntryView.Empty;

    [SerializeField] private Transform playerEntry;
    [SerializeField] private Button addMemberButton;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void switchView(PartyEntryView partyEntryView)
    {
        
    }
}
