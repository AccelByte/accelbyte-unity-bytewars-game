// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Text;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using AccelByte.Models;
using UnityEngine;

public enum PresenceActivity
{
    InMainMenu,
    InAMatch,
    Matchmaking,
    MatchLobby,
    InAParty,
    Elimination,
    TeamDeathmatch,
    Singleplayer
}

public class PresenceHelper : MonoBehaviour
{
    public const string DefaultActivityKeyword = "nil";
    public const string HyphenSeparator = " - ";
    public const string CommaSeparator = ", ";

    public static ReadOnlyDictionary<UserStatus, string> AvailabilityStatus = new(
        new Dictionary<UserStatus, string>
        {
            { UserStatus.Online, "Online" },
            { UserStatus.Offline, "Offline" },
        });

    public static ReadOnlyDictionary<PresenceActivity, string> ActivityStatus = new(
        new Dictionary<PresenceActivity, string>
        {
            { PresenceActivity.InMainMenu, "In Main Menu" },
            { PresenceActivity.InAMatch, "In a Match" },
            { PresenceActivity.Matchmaking, "Matchmaking" },
            { PresenceActivity.MatchLobby, "Lobby" },
            { PresenceActivity.InAParty, "In a Party" },
            { PresenceActivity.Elimination, "Elimination" },
            { PresenceActivity.TeamDeathmatch, "Death Match" },
            { PresenceActivity.Singleplayer, "Singleplayer" },
        });

    public static string GetLastOnline(DateTime lastOnline)
    {
        DateTime currentTime = DateTime.UtcNow;
        StringBuilder lastOnlineText = new StringBuilder("Last online ");
        
        if (currentTime.Year - lastOnline.Year > 0)
        {
            lastOnlineText.Append(currentTime.Year - lastOnline.Year)
                          .Append(currentTime.Year - lastOnline.Year > 1 ? " years" : " year");
        }
        else if (currentTime.Month - lastOnline.Month > 0)
        {
            lastOnlineText.Append(currentTime.Month - lastOnline.Month)
                          .Append(currentTime.Month - lastOnline.Month > 1 ? " months" : " month");
        }
        else if (currentTime.Day - lastOnline.Day > 0)
        {
            lastOnlineText.Append(currentTime.Day - lastOnline.Day)
                          .Append(currentTime.Day - lastOnline.Day > 1 ? " days" : " day");
        }
        else if (currentTime.Hour - lastOnline.Hour > 0)
        {
            lastOnlineText.Append(currentTime.Hour - lastOnline.Hour)
                          .Append(currentTime.Hour - lastOnline.Hour > 1 ? " hours" : " hour");
        }
        else if (currentTime.Minute - lastOnline.Minute > 0)
        {
            lastOnlineText.Append(currentTime.Minute - lastOnline.Minute)
                          .Append(currentTime.Minute - lastOnline.Minute > 1 ? " minutes" : " minute");
        }
        else
        {
            lastOnlineText.Append("a while");
        }

        lastOnlineText.Append(" ago");

        return lastOnlineText.ToString();
    }
}
