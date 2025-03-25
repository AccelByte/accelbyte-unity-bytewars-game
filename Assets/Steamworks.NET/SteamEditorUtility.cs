#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SteamEditorUtility
{
    private static bool _isEditorFocused;
    [InitializeOnLoadMethod]
    private static void InitOnLoad()
    {
        EditorApplication.update += OnUpdate;
    }

    private static async void CheckSteamAppIdConfig()
    {
        var tutorialModuleConfig = (TextAsset)Resources.Load(GConfig.ConfigurationPath);
        if (tutorialModuleConfig == null)
            return;
        var json = JsonUtility.FromJson<TutorialModuleConfig>(tutorialModuleConfig.text);
        if (json.steamConfiguration == null)
            return;
        string newSteamAppId = json.steamConfiguration.steamAppId;
        if (uint.TryParse(newSteamAppId, out var newSteamAppIdUint))
        {
            string strCwdPath = Directory.GetCurrentDirectory();
            string strSteamAppIdPath = Path.Combine(strCwdPath, "steam_appid.txt");
            if(!File.Exists(strSteamAppIdPath))
            {
                try {
                    var appIdFile = File.CreateText(strSteamAppIdPath);
                    await appIdFile.WriteAsync(newSteamAppId);
                    appIdFile.Close();
                    Debug.Log("[SteamEditorUtility] Successfully copied 'steam_appid.txt' into the project root.");
                    return;
                }
                catch (System.Exception e) {
                    Debug.LogWarning("[SteamEditorUtility] Could not copy 'steam_appid.txt' into the project root. Please place 'steam_appid.txt' into the project root manually.");
                    Debug.LogException(e);
                }
            }
            else
            {
                using var sr = new StreamReader(strSteamAppIdPath);
                var content = await sr.ReadLineAsync();
                sr.Close();
                if (!newSteamAppId.Equals(content))
                {
                    try
                    {
                        await File.WriteAllTextAsync(strSteamAppIdPath, newSteamAppId);
                        Debug.Log("update steam_appid to "+newSteamAppId);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("failed to automatically update steam_appid.txt, " +
                                  "please close the file or update it manually " +
                                  $"error: {e.Message}");
                    }
                }
            }
        }
    }

    private static void OnUpdate()
    {
        if (_isEditorFocused!=UnityEditorInternal.InternalEditorUtility.isApplicationActive)
        {
            _isEditorFocused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            if (_isEditorFocused)
            {
                // Debug.Log("editor is focused");
                //EditorWindow.focusedWindow?.ShowNotification(new GUIContent("Welcome back! ^_^ "));
                CheckSteamAppIdConfig();
            }
            else
            {
                // Debug.Log("editor focus lost");
            }
        }
    }
    
}
#endif
