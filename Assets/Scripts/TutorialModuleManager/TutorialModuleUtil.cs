// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Linq;

public static class TutorialModuleUtil
{
    public static bool IsAccelbyteSDKInstalled()
    {
        var typ = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.Name == "AccelBytePlugin"
            select type);
        int classCount = typ.Count();
        if (classCount == 1)
        {
            return true;
        }
        return false;
    }

    public static string GetLocalTimeOffsetFromUTC()
    {
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
        if (offset == TimeSpan.Zero)
        {
            return "UTC";
        }
        
        char prefix = offset < TimeSpan.Zero ? '-' : '+';
        return prefix + offset.ToString("hh\\:mm");
    }
}
