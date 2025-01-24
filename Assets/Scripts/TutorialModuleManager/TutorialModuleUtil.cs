// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;

public static class TutorialModuleUtil
{
    private static bool? overrideDedicatedServerVersion = null;

    public static bool IsAccelbyteSDKInstalled()
    {
        List<string> types = (from assembly in AppDomain.CurrentDomain.GetAssemblies() from type in assembly.GetTypes() where type.Name.Contains("AccelByte") select type.Name).ToList();
        bool isSDKInstalled = types.Select(x => x.Contains("SDK", StringComparison.OrdinalIgnoreCase)).Count() > 0;
        bool isNetworkPluginInstalled = types.Select(x => x.Contains("Network", StringComparison.OrdinalIgnoreCase)).Count() > 0;

        if (!isSDKInstalled) 
        {
            BytewarsLogger.LogWarning("AccelByte SDK is not installed.");
        }
        if (!isNetworkPluginInstalled) 
        {
            BytewarsLogger.LogWarning("AccelByte Networking is not installed.");
        }

        return isSDKInstalled && isNetworkPluginInstalled;
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

    public static string GetLaunchParamValue(string param)
    {
        string[] cmdArgs = Environment.GetCommandLineArgs();
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            cmdArgs = ParrelSync.ClonesManager.GetArgument().Split();
        }
#endif

        string resultStr = string.Empty;
        foreach (string cmdArg in cmdArgs)
        {
            if (cmdArg.Contains(param, StringComparison.OrdinalIgnoreCase))
            {
                resultStr = cmdArg.Replace(param, string.Empty, StringComparison.OrdinalIgnoreCase);
                break;
            }
        }

        return resultStr;
    }

    public static bool IsOverrideDedicatedServerVersion()
    {
        // Immediately get from cache if not null.
        if (overrideDedicatedServerVersion.HasValue)
        {
            return overrideDedicatedServerVersion.Value;
        }

        overrideDedicatedServerVersion = false;

        // Prioritize the launch parameter first.
        const string overrideDSVersionParam = "-OverrideDSVersion=";
        if (bool.TryParse(GetLaunchParamValue(overrideDSVersionParam), out bool paramValue))
        {
            overrideDedicatedServerVersion = paramValue;
            BytewarsLogger.Log($"Launch param sets the override DS version config to {paramValue.ToString().ToUpper()}");
        }
        // Read from the config file.
        else if (ConfigurationReader.Config != null)
        {
            bool configValue = ConfigurationReader.Config.multiplayerDSConfiguration.overrideDSVersion;
            overrideDedicatedServerVersion = configValue;
            BytewarsLogger.Log($"Config file sets the override DS version config to {configValue.ToString().ToUpper()}");
        }

        return overrideDedicatedServerVersion.Value;
    }

    public static ProxyConfiguration GetProxy()
    {
        const string proxyUrlParam = "-ProxyUrl=";
        const string proxyPathParam = "-ProxyPath=";
        const string proxyUsernameParam = "-ProxyUsername=";
        const string proxyPasswordParam = "-ProxyPassword=";

        // Get values from config file.
        ProxyConfiguration proxyConfig = ConfigurationReader.Config.multiplayerDSConfiguration.proxyConfiguration;

        // Override the config file with launch parameter.
        string proxyUrl = GetLaunchParamValue(proxyUrlParam);
        string proxyPath = GetLaunchParamValue(proxyPathParam);
        string proxyUsername = GetLaunchParamValue(proxyUsernameParam);
        string proxyPassword = GetLaunchParamValue(proxyPasswordParam);

        if (!string.IsNullOrEmpty(proxyUrl)) 
        {
            proxyConfig.url = proxyUrl;
        }
        if (!string.IsNullOrEmpty(proxyPath))
        {
            proxyConfig.path = proxyPath;
        }
        if (!string.IsNullOrEmpty(proxyUsername))
        {
            proxyConfig.username = proxyUsername;
        }
        if (!string.IsNullOrEmpty(proxyPassword))
        {
            proxyConfig.password = proxyPassword;
        }
        
        BytewarsLogger.Log(
            $"Proxy config: Url: {proxyConfig.url}, Path: {proxyConfig.path}, " +
            $"Is auth credentials empty: {string.IsNullOrEmpty(proxyConfig.username) || string.IsNullOrEmpty(proxyConfig.password)}");

        return proxyConfig;
    }
}
