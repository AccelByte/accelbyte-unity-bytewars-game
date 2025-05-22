// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using System.Collections.Generic;

public class RegionPreferenceInfo
{
    public readonly static Dictionary<string, string> RegionNames = new Dictionary<string, string>()
    {
        {"us-east-2", "US East (Ohio)"},
        {"us-east-1", "US East (Virginia)"},
        {"us-west-1", "US West (N. California)"},
        {"us-west-2", "US West (Oregon)"},
        {"af-south-1", "Africa (Cape Town)"},
        {"ap-east-1", "Asia Pacific (Hong Kong)"},
        {"ap-south-2", "Asia Pacific (Hyderabad)"},
        {"ap-southeast-3", "Asia Pacific (Jakarta)"},
        {"ap-southeast-4", "Asia Pacific (Melbourne)"},
        {"ap-south-1", "Asia Pacific (Mumbai)"},
        {"ap-northeast-3", "Asia Pacific (Osaka)"},
        {"ap-northeast-2", "Asia Pacific (Seoul)"},
        {"ap-southeast-1", "Asia Pacific (Singapore)"},
        {"ap-southeast-2", "Asia Pacific (Sydney)"},
        {"ap-northeast-1", "Asia Pacific (Tokyo)"},
        {"ca-central-1", "Canada (Central)"},
        {"ca-west-1", "Canada West (Calgary)"},
        {"eu-central-1", "Europe (Frankfurt)"},
        {"eu-west-1", "Europe (Ireland)"},
        {"eu-west-2", "Europe (London)"},
        {"eu-south-1", "Europe (Milan)"},
        {"eu-west-3", "Europe (Paris)"},
        {"eu-south-2", "Europe (Spain)"},
        {"eu-north-1", "Europe (Stockholm)"},
        {"eu-central-2", "Europe (Zurich)"},
        {"il-central-1", "Israel (Tel Aviv)"},
        {"me-south-1", "Middle East (Bahrain)"},
        {"me-central-1", "Middle East (UAE)"},
        {"sa-east-1", "South America (São Paulo)"},
    };

    public string RegionCode = string.Empty;
    public float Latency = 0.0f;
    public bool Enabled = true;
}

public class RegionPreferencesHelper 
{
    public static List<RegionPreferenceInfo> GetEnabledRegions() 
    {
#if UNITY_SERVER
        BytewarsLogger.LogWarning("Bad request. Only game client is allowed to call this function.");
        return null;
#else
        List<RegionPreferenceInfo> enabledRegions = new List<RegionPreferenceInfo>();

        ModuleModel module = TutorialModuleManager.Instance.GetModule(TutorialType.RegionPreferences);
        if (module == null || !module.isActive) 
        {
            BytewarsLogger.LogWarning("Unable to get enabled preferred regions. Region preferences module is invalid.");
            return enabledRegions;
        }

        // Return result based on starter mode status.
        if (module.isStarterActive) 
        {
            // TODO: Create wrapper starter version for tutorial module documentation later.
            BytewarsLogger.LogWarning("TODO: Starter version of region preferences wrapper is not yet created.");
            return enabledRegions;
        }
        else 
        {
            RegionPreferencesWrapper regionPreferencesWrapper = TutorialModuleManager.Instance.GetModuleClass<RegionPreferencesWrapper>();
            if (regionPreferencesWrapper == null)
            {
                BytewarsLogger.LogWarning("Unable to get enabled preferred regions. Region preferences wrapper is null.");
                return enabledRegions;
            }

            return regionPreferencesWrapper.GetEnabledRegions();
        }
#endif
    }

    public static List<SessionV2GameSession> FilterEnabledRegionGameSession(List<SessionV2GameSession> gameSessions)
    {
#if UNITY_SERVER
        BytewarsLogger.LogWarning("Bad request. Only game client is allowed to call this function.");
        return gameSessions;
#else
        ModuleModel module = TutorialModuleManager.Instance.GetModule(TutorialType.RegionPreferences);
        if (module == null || !module.isActive)
        {
            BytewarsLogger.LogWarning("Unable to filter game session based on enabled preferred regions. Region preferences module is invalid.");
            return gameSessions;
        }

        // Return result based on starter mode status.
        if (module.isStarterActive)
        {
            // TODO: Create wrapper starter version for tutorial module documentation later.
            BytewarsLogger.LogWarning("TODO: Starter version of region preferences wrapper is not yet created.");
            return gameSessions;
        }
        else
        {
            RegionPreferencesWrapper regionPreferencesWrapper = TutorialModuleManager.Instance.GetModuleClass<RegionPreferencesWrapper>();
            if (regionPreferencesWrapper == null)
            {
                BytewarsLogger.LogWarning("Unable to filter game session based on enabled preferred regions. Region preferences wrapper is null.");
                return gameSessions;
            }

            return regionPreferencesWrapper.FilterEnabledRegionGameSession(gameSessions);
        }
#endif
    }
}