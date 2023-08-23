using System;
using System.Linq;

public class GConfig
{
    public const string ConfigurationPath = "Modules/TutorialModuleConfig";
    public static string GetSteamAppId()
    {
        var steamConfig = GetSteamConfiguration();
        if (steamConfig != null)
        {
            return steamConfig.steamAppId;
        }
        return "";
    }

    public static bool GetSteamAutoLogin()
    {
        var steamConfig = GetSteamConfiguration();
        if (steamConfig != null)
        {
            return steamConfig.autoLogin;
        }
        return true;
    }
    public static bool IsSteamEnabled()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("-nosteam"))
        {
            return false;
        }
        return true;
    }
    private static SteamConfiguration GetSteamConfiguration()
    {
        if (ConfigurationReader.Config != null)
        {
            return ConfigurationReader.Config.steamConfiguration;
        }
        return null;
    }
}