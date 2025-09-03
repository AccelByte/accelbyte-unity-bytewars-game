// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class TutorialModuleForceEnable
{
    internal const string FIRST_TIME = "FIRST_TIME"; // the key
    private const string CachedTutorialModuleConfigJson = "CachedConfig";

    public static string[] ListAllModules
    {
        get => _moduleDependencies;
        set => _moduleDependencies = value;
    }
    public static string[] ForcedModules
    {
        get => _forcedModules;
        set => _forcedModules = value;
    }

    public static bool IsForceDisable
    {
        get => _isForceDisableOtherModules;
        set => _isForceDisableOtherModules = value;
    }

    private static TutorialModuleData _overrideModule;
    private static string[] _forcedModules;
    private static bool _isError;
    private static Dictionary<string, TutorialModuleData> _moduleDictionary = new Dictionary<string, TutorialModuleData>();
    private static string[] _moduleDependencies;
    private static bool _isForceDisableOtherModules;
    private static bool _isUnityEditorFocused;

    static TutorialModuleForceEnable()
    {
        EditorApplication.update += RunOnce;
        EditorApplication.update += OnUpdate;
        EditorApplication.quitting += Quit;
    }

    private static void OnUpdate()
    {
        if (_isUnityEditorFocused != UnityEditorInternal.InternalEditorUtility.isApplicationActive)
        {
            _isUnityEditorFocused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
            if (_isUnityEditorFocused)
            {
                CheckJson();
            }
        }
    }

    private static void CheckJson()
    {
        string textJson = ReadJson();
        string cachedJsonString = EditorPrefs.GetString(CachedTutorialModuleConfigJson);
        if (string.IsNullOrEmpty(cachedJsonString) || !cachedJsonString.Equals(textJson))
        {
            EditorPrefs.SetString(CachedTutorialModuleConfigJson, textJson);

            UpdateConfigFromJson(textJson);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void Quit()
    {
        EditorPrefs.DeleteKey(FIRST_TIME);
        EditorPrefs.DeleteKey(CachedTutorialModuleConfigJson);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Method to show window at the firs time open editor
    /// </summary>
    private static void RunOnce()
    {
        var firstTime = EditorPrefs.GetBool(FIRST_TIME, true);
        if (firstTime)
        {
            if (EditorPrefs.GetBool("FIRST_TIME", true))
            {
                UpdateConfigFromJson(ReadJson());
            }

            EditorPrefs.SetBool(FIRST_TIME, false);
        }

        EditorApplication.update -= RunOnce;
    }

    private static bool UpdateConfigFromJson(string jsonStr)
    {
        _moduleDictionary.Clear();
#if UNITY_EDITOR
        // Reset tutorial module values to its original Scriptable Object state.
        // Only do this on editor, since the values are automatically reset when reopening the project and on packaged game.
        ResetAllModuleState();
#endif

        string[] isReadJsonConfig = ReadJsonConfig(jsonStr) != null ? ReadJsonConfig(jsonStr) : null;
        if (isReadJsonConfig != null)
        {
            isReadJsonConfig.ToList().ForEach(x => ForceEnableModules($"{x}AssetConfig", true));

            _moduleDependencies = _moduleDictionary.Select(x => x.Key.Replace("AssetConfig", "")).ToArray();
            _moduleDependencies.ToList().ForEach(ForceEnable);
            if (_isForceDisableOtherModules)
            {
                DisableRestOfModules(_moduleDependencies);
            }

            ShowPopupForceEnable.Init();

            return true;
        }
        return false;
    }

    private static bool IsTargetModuleCurrentSelectedModule()
    {
        return _overrideModule.name == Selection.activeObject.name ? true : false;
    }

    public static bool IsDependency(string selectedAssetConfig)
    {
        // Check each asset configs registered in _moduleDictionary
        // Debug.Log($"check if it's a dependency module {selectedAssetConfig}");
        _moduleDictionary.TryGetValue(selectedAssetConfig, out _overrideModule);
        if (_overrideModule != null)
        {
            // Debug.Log($"{_overrideModule.name} it's a dependency module ");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Read Json config related to the override modules
    /// </summary>
    /// <param name="readFromInspector"></param>
    /// <param name="startOrChangeConfig"></param>
    /// <returns></returns>
    private static string[] ReadJsonConfig(string jsonStr, bool readFromInspector = false)
    {
        var json = JsonUtility.FromJson<TutorialModuleConfig>(jsonStr);
        // Check if open asset config from inspector
        if (!readFromInspector)
        {
            Debug.Log($"override module {String.Join(" ", json.forceEnabledModules)}");
            Debug.Log($"override status {json.enableModulesOverride}");
        }
        if (json.forceEnabledModules.Length <= 0)
        {
            Debug.Log($"there are no modules override, check length module {json.forceEnabledModules.Length}");
            _forcedModules = null;
            return _forcedModules;
        }
        if (!json.enableModulesOverride)
        {
            Debug.Log($"enableModulesOverride status {json.enableModulesOverride}");
            _forcedModules = null;
            return _forcedModules;
        }

        _forcedModules = json.forceEnabledModules;
        _isForceDisableOtherModules = json.forceDisabledOtherModules;
        return _forcedModules;
    }

    private static string ReadJson()
    {
        var tutorialModuleConfig = (TextAsset)Resources.Load("Modules/TutorialModuleConfig");
        var textJson = tutorialModuleConfig.text;
        return textJson;
    }

    private static void ForceEnable(string moduleName)
    {
        TutorialModuleData module = GetTutorialModuleDataObject(moduleName);
        if (module == null)
        {
            BytewarsLogger.LogWarning($"Unable to force enable {moduleName}. Tutorial module is not found.");
            return;
        }

        module.CacheState();
        module.isActive = true;
    }

    public static bool ForceEnableModules(string moduleName, bool isFirstTime = false)
    {
        if (!moduleName.ToLower().Contains("assetconfig"))
        {
            return false;
        }

        string[] overridesModules =
            ReadJsonConfig(ReadJson(), readFromInspector: true) != null ? _forcedModules : null;
        if (overridesModules == null || overridesModules.Length <= 0)
        {
            _moduleDictionary.Clear();
            return false;
        }

        bool overrideStatus = false;
        Dictionary<string, bool> modulesDictionary = new Dictionary<string, bool>();
        overridesModules?.ToList().ForEach(x =>
        {
            TutorialModuleData module = GetTutorialModuleDataObject(x);
            if (module == null)
            {
                overrideStatus = false;
                return;
            }

            _overrideModule = module;

            if (!isFirstTime)
            {
                if (IsTargetModuleCurrentSelectedModule())
                {
                    _overrideModule.CacheState();
                    _overrideModule.isActive = true;
                    overrideStatus = SetDependenciesToActive();
                }
                else
                {
                    overrideStatus = false;
                }
            }
            else
            {
                overrideStatus = true;
            }

            modulesDictionary.Add(_overrideModule.name, overrideStatus);
            _moduleDictionary.TryAdd(_overrideModule.name, _overrideModule);

            CheckDependency(_overrideModule);
        });
        modulesDictionary.TryGetValue(moduleName, out overrideStatus);

        return overrideStatus;
    }

    public static bool IsForceEnableModule(string moduleName)
    {
        return _moduleDictionary.ContainsKey(moduleName);
    }

    private static void ResetAllModuleState()
    {
        string[] modulePaths = AssetDatabase.FindAssets("AssetConfig").Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach (string path in modulePaths)
        {
            TutorialModuleData module = AssetDatabase.LoadAssetAtPath<TutorialModuleData>(path);
            module.RevertState();
        }
    }

    private static void DisableRestOfModules(string[] modules)
    {
        string[] allModulePaths = AssetDatabase
            .FindAssets("t:TutorialModuleData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith("AssetConfig"))
            .ToArray();

        HashSet<string> targetFileNames = modules.Select(m => m + "AssetConfig").ToHashSet();

        string[] modulesToKeepEnabled = allModulePaths.Where(path =>
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            return targetFileNames.Contains(fileName);
        }).ToArray();

        string[] modulesToDisable = allModulePaths.Except(modulesToKeepEnabled).ToArray();
        foreach (string path in modulesToDisable)
        {
            TutorialModuleData module = AssetDatabase.LoadAssetAtPath<TutorialModuleData>(path);
            if (module != null)
            {
                module.CacheState();
                module.isActive = false;
            }
        }
    }

    private static void CheckDependency(TutorialModuleData moduleData)
    {
        foreach (var tutorialModuleData in moduleData.moduleDependencies)
        {
            _moduleDictionary.TryAdd(tutorialModuleData.name, tutorialModuleData);
            if (tutorialModuleData.moduleDependencies != null)
            {
                CheckDependency(tutorialModuleData);
            }
        }
    }

    private static bool SetDependenciesToActive()
    {
        if (_overrideModule.moduleDependencies.Length <= 0)
        {
                return true;
        }

        int index = 0;
        foreach (TutorialModuleData moduleDependency in _overrideModule.moduleDependencies)
        {
            if (moduleDependency == null)
            {
                BytewarsLogger.Log($"Module dependency on index {index} is null. Please check the module dependency value.");
                return false;
            }

            moduleDependency.CacheState();
            moduleDependency.isActive = true;
            moduleDependency.isStarterActive = false;

            index++;
        }
        return true;
    }

    private static TutorialModuleData GetTutorialModuleDataObject(string moduleName)
    {
        string targetFileName = $"{moduleName}AssetConfig";

        string path = AssetDatabase
            .FindAssets("t:TutorialModuleData")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == targetFileName);

        if (!string.IsNullOrEmpty(path))
        {
            BytewarsLogger.Log($"Found tutorial module data at path: {path}");
            return AssetDatabase.LoadAssetAtPath<TutorialModuleData>(path);
        }

        BytewarsLogger.LogWarning($"Tutorial module data with '{targetFileName}' name not found.");
        return null;
    }

    private static void GetAllHelperFiles(Object selectedGameObject, bool isStaterActive = false)
    {
        var assetConfig = (TutorialModuleData)selectedGameObject;
        var fileName = selectedGameObject.name;
        var allModuleFiles = Directory.GetFiles($"{Application.dataPath}/Resources",
            fileName, SearchOption.AllDirectories).Where(x => !x.Contains("UI"));

        var moduleFiles = allModuleFiles as string[] ?? allModuleFiles.ToArray();

        if (isStaterActive)
        {
            //return all starter helper files
            var starterHelperScripts = moduleFiles.Where(x => x.Contains("starter")).Select(AssetDatabase.LoadAssetAtPath<TextAsset>).ToArray();
            assetConfig.starterHelperScripts = starterHelperScripts;

        }
        //return default helper files
        var defaultHelperScripts = moduleFiles.Where(x => !x.Contains("starter")).Select(AssetDatabase.LoadAssetAtPath<TextAsset>).ToArray();
        assetConfig.defaultHelperScripts = defaultHelperScripts;

    }
}
#endif


public class TutorialModuleConfig
{
    public bool enableModulesOverride;
    public string[] forceEnabledModules;
    public bool forceDisabledOtherModules;
    public string singlePlatformAuth;
    public SteamConfiguration steamConfiguration;
    public MultiplayerDSConfiguration multiplayerDSConfiguration;
    public bool useAutoGeneratedDeviceIDForLogin;
    public InGameLatencyConfiguration inGameLatencyConfiguration;
}

[Serializable]
public class SteamConfiguration
{
    public string steamAppId;
    public bool autoLogin;
}

[Serializable]
public class MultiplayerDSConfiguration
{
    public bool isServerUseAMS = false;
    public bool overrideDSVersion = false;
    public ProxyConfiguration proxyConfiguration;
}

[Serializable]
public class ProxyConfiguration
{
    public string url = string.Empty;
    public string path = "/";
    public string username = string.Empty;
    public string password = string.Empty;
}

[Serializable]
public class InGameLatencyConfiguration
{
    public bool showLatency;
    public float latencyRefreshInterval;
}