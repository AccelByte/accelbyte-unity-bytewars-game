// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SDKConfigBuilder
{
    static SDKConfigBuilder()
    {
        // Always reassign delegate to avoid duplicates.
        Builder.OnGenerateSDKConfig -= GenerateSDKConfig;
        Builder.OnGenerateSDKConfig += GenerateSDKConfig;
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
