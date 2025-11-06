// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public class Builder
{
    public static event Action OnGenerateSDKConfig = delegate { };

    private static readonly string[] scenes = new[]
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/GalaxyWorld.unity"
    };
    
    [MenuItem("Build/Build Windows64 Client/Development Build")]
    public static void BuildWindowsClientDevelopment()
    {
        BuildWindowsClient(true);
    }

    [MenuItem("Build/Build Windows64 Client/Release Build")]
    public static void BuildWindowsClientRelease()
    {
        BuildWindowsClient(false);
    }

    private static void BuildWindowsClient(bool development)
    {
        GenerateSDKConfig();
        
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
            options = development ? BuildOptions.Development : BuildOptions.None
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

    [MenuItem("Build/Build Server/Development Build")]
    public static void BuildLinuxServerDevelopment()
    {
        BuildLinuxServer(true);
    }

    [MenuItem("Build/Build Server/Release Build")]
    public static void BuildLinuxServerRelease()
    {
        BuildLinuxServer(false);
    }

    private static void BuildLinuxServer(bool development)
    {
        GenerateSDKConfig();

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
            options = development ? BuildOptions.Development : BuildOptions.None
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
        OnGenerateSDKConfig.Invoke();
    }
}