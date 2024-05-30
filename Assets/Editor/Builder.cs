// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.IO;
using AccelByte.Models;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    private static readonly string[] scenes = new[]
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/GalaxyWorld.unity"
    };
    
    [MenuItem("Build/Build Windows64 Client")]
    public static void BuildWindowsClient()
    {
        string[] cmdArgs = System.Environment.GetCommandLineArgs();
        string locationPathName = "../Build/Client/ByteWars.exe";
        foreach (string arg in cmdArgs)
        {
            if (arg.Contains("-setBuildPath="))
            {
                string buildPath = arg.Replace("-setBuildPath=", "");
                locationPathName = buildPath;
            }
        }

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Builder.BuildWindowsClient] Build client successful - Build written to: {options.locationPathName}");
        }
        else if(report.summary.result == BuildResult.Failed)
        {
            Debug.LogError("[Builder.BuildWindowsClient] Build client failed");
        }
    }
    
    [MenuItem("Build/Build Server")]
    public static void BuildLinuxServer()
    {
        string[] cmdArgs = System.Environment.GetCommandLineArgs();
        string locationPathName = "../Build/Server/ByteWarsServer.x86_64";
        
        foreach (string arg in cmdArgs)
        {
            if (arg.Contains("-setBuildPath="))
            {
                string buildPath = arg.Replace("-setBuildPath=", "");
                locationPathName = buildPath;
            }
        }

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.LinuxHeadlessSimulation, BuildTarget.StandaloneLinux64);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Builder.BuildLinuxServer] Build server successful - Build written to: {options.locationPathName}");
        }
        else if(report.summary.result == BuildResult.Failed)
        {
            Debug.LogError("[Builder.BuildLinuxServer] Build server failed");
        }
    }

    public static void UpdateGameVersion()
    {
        string[] cmdArgs = System.Environment.GetCommandLineArgs();
        string gameVersion = "";

        foreach (string arg in cmdArgs)
        {
            if (arg.Contains("-setGameVersion="))
            {
                gameVersion = arg.Replace("-setGameVersion=", "");
                PlayerSettings.bundleVersion = gameVersion;
            }
        }
    }

    public static void GenerateSDKConfig()
    {
        string[] cmdArgs = System.Environment.GetCommandLineArgs();
        MultiConfigs multiConfigs = new MultiConfigs();
        Config config = new Config();
        bool isServer = false;

        foreach (string arg in cmdArgs)
        {
            if (arg.Contains("-namespace="))
            {
                string agsNamespace = arg.Replace("-namespace=", "");
                config.Namespace = agsNamespace;
                config.Expand(true);
            }

            if (arg.Contains("-baseUrl="))
            {
                string baseUrl = arg.Replace("-baseUrl=", "");
                config.BaseUrl = baseUrl;
                config.Expand(true);
            }

            if (arg.Contains("-redirectUri="))
            {
                string redirectUri = arg.Replace("-redirectUri=", "");
                config.RedirectUri = redirectUri;
                config.Expand(true);
            }

            if (arg.Contains("-publisherNamespace="))
            {
                string publisherNamespace = arg.Replace("-publisherNamespace=", "");
                config.PublisherNamespace = publisherNamespace;
                config.Expand(true);
            }

            if (arg.Contains("-server="))
            {
                bool isForServer = bool.Parse(arg.Replace("-server=", ""));
                isServer = isForServer;
            }
        }
        config.EnableAmsServerQos = true;
        multiConfigs.Default = config;
        multiConfigs.Expand(true);
        
        string fileName = isServer ? "AccelByteServerSDKConfig.json" : "AccelByteSDKConfig.json";
        string json = JsonConvert.SerializeObject(multiConfigs);
        File.WriteAllText($"Assets/Resources/{fileName}", json);
        Debug.Log($"[Builder.GenerateSDKConfigJSON] Generate JSON Assets/Resources/{fileName}");

        GenerateOAuthConfig();
    }

    public static void GenerateOAuthConfig()
    {
        string[] cmdArgs = System.Environment.GetCommandLineArgs();
        MultiOAuthConfigs multiOAuthConfig = new MultiOAuthConfigs();
        OAuthConfig oauthConfig = new OAuthConfig();
        bool isServer = false;

        foreach (string arg in cmdArgs)
        {
            if (arg.Contains("-clientId="))
            {
                string clientId = arg.Replace("-clientId=", "");
                oauthConfig.ClientId = clientId;
                oauthConfig.Expand();
            }

            if (arg.Contains("-clientSecret="))
            {
                string clientSecret = arg.Replace("-clientSecret=", "");
                oauthConfig.ClientSecret = clientSecret;
                oauthConfig.Expand();
            }

            if (arg.Contains("-server="))
            {
                bool isForServer = bool.Parse(arg.Replace("-server=", ""));
                isServer = isForServer;
            }
        }

        multiOAuthConfig.Default = oauthConfig;
        multiOAuthConfig.Expand();

        string fileName = isServer ? "AccelByteServerSDKOAuthConfig.json" : "AccelByteSDKOAuthConfig.json";
        string json = JsonConvert.SerializeObject(multiOAuthConfig);
        File.WriteAllText($"Assets/Resources/{fileName}", json);
        Debug.Log($"[Builder.GenerateSDKOAuthJSON] Generate JSON Assets/Resources/{fileName}");
    }
}
