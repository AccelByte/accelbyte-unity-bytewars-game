// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

public class Utilities
{
    public static string TryParseLocalHostUrl(string url) 
    {
        return Regex.Replace(url, "localhost", "127.0.0.1", RegexOptions.IgnoreCase);
    }

    public static string GetNowDate()
    {
        var now = DateTime.Now;
        var dateStr = now.ToString("MM/dd/yyyy hh:mm:ss.F", CultureInfo.InvariantCulture);
        return dateStr;
    }
}
