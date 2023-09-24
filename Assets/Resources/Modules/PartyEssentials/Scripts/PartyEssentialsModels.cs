using UnityEngine;

public struct PartyMemberData
{
    public readonly string UserId;
    public readonly string DisplayName;
    public readonly Texture2D Avatar;
    
    public PartyMemberData(string userId, string displayName, Texture2D avatar)
    {
        this.UserId = userId;
        this.DisplayName = displayName;
        this.Avatar = avatar;
    }
}

public enum PartyEntryView
{
    Empty,
    MemberInfo
}