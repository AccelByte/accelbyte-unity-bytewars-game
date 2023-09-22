using UnityEngine;

public struct PartyMemberData
{
    public readonly string DisplayName;
    public readonly Texture2D AvatarUrl;
    
    public PartyMemberData(string playerDisplayName, Texture2D playerAvatarUrl)
    {
        DisplayName = playerDisplayName;
        AvatarUrl = playerAvatarUrl;
    }
}

public enum PartyEntryView
{
    Empty,
    MemberInfo
}