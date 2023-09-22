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

    public static bool IsError
    {
        get => _isError;
        set => _isError = value;
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
        if (_isUnityEditorFocused!=UnityEditorInternal.InternalEditorUtility.isApplicationActive)
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
        var textJson = ReadJson();
        var cachedJsonString = EditorPrefs.GetString(CachedTutorialModuleConfigJson);
        if (String.IsNullOrEmpty(cachedJsonString) || 
            !cachedJsonString.Equals(textJson))
        {
            EditorPrefs.SetString(CachedTutorialModuleConfigJson, textJson);
            UpdateConfigFromJson(textJson);
        }
    }

    private static void Quit()
    {
        EditorPrefs.DeleteKey(FIRST_TIME);
        EditorPrefs.DeleteKey(CachedTutorialModuleConfigJson);
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
                if (UpdateConfigFromJson(ReadJson()))
                {
                    Debug.Log($"first time opened");
                    ShowPopupForceEnable.Init();
                }
            }

            EditorPrefs.SetBool(FIRST_TIME, false);
        }

        EditorApplication.update -= RunOnce;
    }

    private static bool UpdateConfigFromJson(string jsonStr)
    {
        var isReadJsonConfig = ReadJsonConfig(jsonStr) != null ? ReadJsonConfig(jsonStr) : null;
        if (isReadJsonConfig != null)
        {
            // GetAllDependencies(isReadJsonConfig);
            isReadJsonConfig.ToList().ForEach(x => ForceEnableModules($"{x}AssetConfig", true));


            _moduleDependencies = _moduleDictionary.Select(x => x.Key.Replace("AssetConfig", "")).ToArray();
            _moduleDependencies.ToList().ForEach(ForceEnable);
            if (_isForceDisableOtherModules)
            {
                DisableRestOfModules(_moduleDependencies);
            }
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
            Debug.Log($"override module {String.Join(" ",json.forceEnabledModules)}");
            Debug.Log($"override status {json.enableModulesOverride}");
        }
        if (json.forceEnabledModules.Length <= 0)
        {
            Debug.Log($"there are no modules override, check length module {json.forceEnabledModules.Length}");
            _forcedModules = null;
            _isError = true;
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
        var module = GetTutorialModuleDataObject(moduleName);
        if (module == null)
        {
            return;
        }

        module.isActive = true;
    }

    public static bool ForceEnableModules(string moduleName, bool isFirstTime = false)
    {
        if (!moduleName.ToLower().Contains("assetconfig"))
        {
            return false;
        }

        var jsonStr = ReadJson();
        var isReadJsonConfig = ReadJsonConfig(jsonStr, readFromInspector:true) != null ? _forcedModules : null;

        if (isReadJsonConfig == null)
        {
            return false;
        }
        
        //TODO: Null checcking on moduleName
        var overrideStatus = false;
        var overridesModules = isReadJsonConfig;
        var modulesDictionary = new Dictionary<string, bool>();
        overridesModules?.ToList().ForEach(x =>
        {
            var module = GetTutorialModuleDataObject(x);
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

    private static void DisableRestOfModules(string[] modules)
    {
        var tutorialModules = AssetDatabase.FindAssets("AssetConfig").Select(AssetDatabase.GUIDToAssetPath);
        var forceEnableModule = modules.Select(module => AssetDatabase.FindAssets($"{module}AssetConfig").FirstOrDefault()).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var tutorialModule = tutorialModules.Except(forceEnableModule);
        Debug.Log(tutorialModule.Count());
        foreach (var tutorialModulePath in tutorialModule)
        {
            var tutorialModuleData = AssetDatabase.LoadAssetAtPath<TutorialModuleData>(tutorialModulePath);
            tutorialModuleData.isActive = false;
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
        var modulesLength = _overrideModule.moduleDependencies.Length;
        switch (modulesLength)
        {
            case 0:
                return true;
            case > 0:
            {
                foreach (var dependency in _overrideModule.moduleDependencies.Select((value, index) =>
                             (value, index)))
                {
                    if (dependency.value == null)
                    {
                        Debug.Log($"this element {dependency.index} is null, please add the dependency value");
                        return false;
                    }

                    Debug.Log($"element {dependency.index} {dependency.value.name}");
                    dependency.value.isActive = true;
                    dependency.value.isStarterActive = false;
                }

                return true;
            }
            default:
                return false;
        }
    }

    private static TutorialModuleData GetTutorialModuleDataObject(string moduleName)
    {
        var fileName = $"{moduleName}AssetConfig";
        var assets = AssetDatabase.FindAssets(fileName).FirstOrDefault();
        if (assets != null && assets.Length == 0)
        {
            Debug.Log($"check your module name, the Asset Config cannot be found");
            _isError = true;
            ShowPopupForceEnable.Init();
            return null;
        }
        var asset = assets != null
            ? AssetDatabase.GUIDToAssetPath(assets)
            : null;
        Debug.Log(asset);
        return AssetDatabase.LoadAssetAtPath<TutorialModuleData>(asset);
    }

    private static void GetAllHelperFiles(Object selectedGameObject, bool isStaterActive = false)
    {
        var assetConfig = (TutorialModuleData)selectedGameObject;
        var fileName = selectedGameObject.name;
        var allModuleFiles = Directory.GetFiles($"{Application.dataPath}/Resources", 
            fileName, SearchOption.AllDirectories ).Where(x => !x.Contains("UI"));

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
    public SteamConfiguration steamConfiguration;
    public MultiplayerDSConfiguration multiplayerDSConfiguration;
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
    public bool isServerUseAMS ;
}
