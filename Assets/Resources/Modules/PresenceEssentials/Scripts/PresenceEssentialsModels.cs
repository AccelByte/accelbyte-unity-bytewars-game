// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AccelByte.Models;

public class PresenceEssentialsModels 
{
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

    public const string DefaultActivityKeyword = "No Status";
    public const string HyphenSeparator = " - ";
    public const string CommaSeparator = ", ";

    public static readonly ReadOnlyDictionary<UserStatus, string> AvailabilityStatus =
        new ReadOnlyDictionary<UserStatus, string>(new Dictionary<UserStatus, string>
        {
            { UserStatus.Online, "Online" },
            { UserStatus.Offline, "Offline" },
        });

    public static readonly ReadOnlyDictionary<PresenceActivity, string> ActivityStatus =
        new ReadOnlyDictionary<PresenceActivity, string>(new Dictionary<PresenceActivity, string>
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

        int yearDiff = currentTime.Year - lastOnline.Year;
        int monthDiff = currentTime.Month - lastOnline.Month;
        int dayDiff = currentTime.Day - lastOnline.Day;
        int hourDiff = currentTime.Hour - lastOnline.Hour;
        int minuteDiff = currentTime.Minute - lastOnline.Minute;

        StringBuilder lastOnlineText = new StringBuilder("Last Online ");
        if (yearDiff > 0)
        {
            lastOnlineText.Append(yearDiff).Append(" Year(s)");
        }
        else if (monthDiff > 0)
        {
            lastOnlineText.Append(monthDiff).Append(" Month(s)");
        }
        else if (dayDiff > 0)
        {
            lastOnlineText.Append(dayDiff).Append(" Day(s)");
        }
        else if (hourDiff > 0)
        {
            lastOnlineText.Append(hourDiff).Append(" Hour(s)");
        }
        else if (minuteDiff > 0)
        {
            lastOnlineText.Append(minuteDiff).Append(" Minute(s)");
        }
        else
        {
            lastOnlineText.Append("a While");
        }

        return lastOnlineText.Append(" Ago").ToString();
    }
}
