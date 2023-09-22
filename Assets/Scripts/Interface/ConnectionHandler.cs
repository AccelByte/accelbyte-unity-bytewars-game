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

    public static bool GetArgument()
    {
        var args = GetCommandlineArgs();

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

            return true;
        }

        if (args.ContainsKey("-cloud_ds"))
            return false;
        #if UNITY_EDITOR
                LocalServerName = SystemInfo.deviceName;
                LocalServerIP = GetLocalIPAddress();
                return true;
        #endif

        return false;
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
    
    public static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "0.0.0.0";
    }
}