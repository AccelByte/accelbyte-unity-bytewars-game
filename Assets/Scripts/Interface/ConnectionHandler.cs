// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ConnectionHandler
{
    public static string LocalServerName;
    public static string LocalServerIP;
    public const ushort DefaultPort = 7778;
    private static bool isInitialized;
    private static bool isUsingLocalDS;

    public static void Initialization()
    {
        if (isInitialized)
        {
            return;
        }
        Dictionary<string, string> args = GetCommandlineArgs();
#if UNITY_WEBGL
        args = GetURLParameters();
#endif
        if (args.TryGetValue("-localserver", out string servername))
        {
            LocalServerName = servername;
            if (args.TryGetValue("-local_ip", out string localIp) && !String.IsNullOrEmpty(localIp))
            {
                LocalServerIP = localIp;
            }
            else
            {
                LocalServerIP = GetLocalIPAddress();
            }
            isUsingLocalDS = true;
        }
        isInitialized = true;
    }

    public static Dictionary<string, string> GetCommandlineArgs()
    {
        Dictionary<string, string> argDictionary = new Dictionary<string, string>();
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; ++i)
        {
            var arg = args[i].ToLower();
            if (arg.StartsWith("-"))
            {
                var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                value = (value?.StartsWith("-") ?? false) ? null : value;

                argDictionary.Add(arg, value);
            }
        }
        return argDictionary;
    }

    public static Dictionary<string, string> GetURLParameters()
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        string url = Application.absoluteURL;

        if (url.Contains("?"))
        {
            string queryString = url.Substring(url.IndexOf('?') + 1);
            string[] pairs = queryString.Split('&');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0]] = keyValue[1];
                }
            }
        }

        return parameters;
    }

    public static string GetLocalIPAddress()
    {
#if !UNITY_WEBGL
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
#endif
        return "0.0.0.0";
    }

    public static bool IsUsingLocalDS()
    {
        return isUsingLocalDS;
    }

    public static ushort GetPort()
    {
        Dictionary<string, string> args = GetCommandlineArgs();
        if (args.TryGetValue("-port", out string portStr))
        {
            if (ushort.TryParse(portStr, out ushort portUshort))
            {
                return portUshort;
            }
        }
        return DefaultPort;
    }
}