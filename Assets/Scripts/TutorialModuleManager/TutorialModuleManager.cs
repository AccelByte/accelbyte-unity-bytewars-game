using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class TutorialModuleManager : MonoBehaviour
{
    // private instance
    private static TutorialModuleManager _instance;
    // instance getter
    public static TutorialModuleManager Instance => _instance;
    public bool IsInstantiated => _isInstantiated;
    public Dictionary<TutorialType, GameObject> InstantiatedTutorials => _instantiatedTutorialPrefabs;

    private Dictionary<string, ModuleData> _moduleClassTypes = new Dictionary<string, ModuleData>();
    private Dictionary<string, List<Transform>> _moduleUITransforms = new Dictionary<string, List<Transform>>();
    private bool _isInstantiated = false;
    private Dictionary<TutorialType, GameObject> _instantiatedTutorialPrefabs = new Dictionary<TutorialType, GameObject>();

    private void Awake()
    {
        // check if another instance for TutorialModuleManager exists
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        
        // PrepareScriptAssets();
        PrepareScriptFromAssetConfig();
    }

    private void Start()
    {
        AddModuleComponents();
    }
    
    #region Runtime Initialize Functions
    
    /// <summary>
    /// Create TutorialModuleManager GameObject in Scene on Runtime
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    private static void SingletonInstanceChecker()
    {
        if (_instance == null)
        {
            GameObject moduleManagerGameObject = new GameObject("TutorialModuleManager");
            _instance = moduleManagerGameObject.AddComponent<TutorialModuleManager>();
        }
    }
    
    #endregion

    #region Module Config Functions
    
    public ModuleModel GetModule(TutorialType tutorialType)
    {
        var tutorialModules = AssetManager.Singleton.GetTutorialModules();
        
        if (tutorialModules.TryGetValue(tutorialType, out TutorialModuleData moduleData))
        {
            var moduleModel = IsStarterActive(moduleData);
            return moduleModel;
        }
        else
        {
            return null;
        }
    }

    private readonly Dictionary<TutorialType, ModuleModel> activeModules =
        new Dictionary<TutorialType, ModuleModel>();
    public Dictionary<TutorialType, ModuleModel> GetAllActiveModule() 
    {
        if (AssetManager.Singleton)
        {
            var tutorialModules = AssetManager.Singleton.GetTutorialModules();
            foreach (var tModule in tutorialModules)
            {
                if (tModule.Value.isActive)
                {
                    var module = IsStarterActive(tModule.Value);
                    activeModules.TryAdd(module.type, module);
                }
            }
        }
        return activeModules;
    }

    public bool IsModuleActive(TutorialType type)
    {
        if (activeModules.TryGetValue(type, out var moduleData))
        {
            return moduleData.isActive;
        }
        return false;
    }


    // private void InstantiateTutorialUI()
    // {
    //     var tutorialModules = AssetManager.Singleton.GetTutorialModules();
    //     foreach (var tModule in tutorialModules)
    //     {
    //         if (tModule.Value.isActive)
    //         {
    //             var instantiatedPrefab =
    //                 Instantiate(tModule.Value.prefab, Vector3.zero, Quaternion.identity, transform);
    //             instantiatedPrefab.gameObject.SetActive(false);
    //             if (_instantiatedTutorialPrefabs.TryGetValue(tModule.Key, out var existingGameObject))
    //             {
    //                 Debug.Log($"tutorial {tModule.Value.prefab.name} has been added");
    //             }
    //             else
    //             {
    //                 _instantiatedTutorialPrefabs.Add(tModule.Key, instantiatedPrefab.gameObject);
    //             }
    //         }
    //         else
    //         {
    //             Debug.Log($"module {tModule.Value.type} is exists but inactive");
    //         }
    //     }
    //     _isInstantiated = true;
    // }

    public T GetTutorialUIHandler<T>(TutorialType tutorialType)
    {
        if(_instantiatedTutorialPrefabs.TryGetValue(tutorialType, out var instantiatedUI))
        {
            return instantiatedUI.GetComponent<T>();
        }
        else
        {
            Debug.Log($"value with key {tutorialType} does not exist");
        }
        return default(T);
    }
    
    #endregion

    #region Module References Initialization Functions

    /// <summary>
    /// Prepare Script Asset for Module Class Reference
    /// </summary>
    private void PrepareScriptAssets()
    {
        // temporary added, expected result from AssetManager (need to be added in AssetManager instead)
        if (AssetManager.Singleton == null)
            return;
        Object[] scriptObjects = AssetManager.Singleton.GetTextAssets();
        
        // store the Class Type values in dictionary
        foreach (Object script in scriptObjects)
        {
            Type scriptClassType = TypeBuilder.GetType(script.name);

            if (scriptClassType != null)
            {
                string scriptPath = Path.GetFullPath(script.name + ".cs");
                List<string> pathCategories = scriptPath.Split(new char[] {'\\', '/'}).ToList();
                
                #if UNITY_STANDALONE
                    if (script.name.Contains("Handler")) continue;
                #endif
                
                if (pathCategories.Contains("UI")) continue;
                
                // remove all folders path until "Modules" folder
                pathCategories.RemoveRange(0, pathCategories.IndexOf("Modules") + 1);
                // remove script name
                pathCategories.RemoveAt(pathCategories.Count - 1);
                // remove "Scripts" folder
                if (pathCategories.Contains("Scripts"))
                {
                    pathCategories.RemoveAt(pathCategories.IndexOf("Scripts"));
                }

                _moduleClassTypes.Add(script.name, new ModuleData(scriptClassType, pathCategories.ToArray()));   
            }
        }
    }
    
    private void PrepareScriptFromAssetConfig()
    {
        // temporary added, expected result from AssetManager (need to be added in AssetManager instead)
        if (AssetManager.Singleton == null)
            return;
        var tutorialModules = AssetManager.Singleton.GetTutorialModules();

        // loop through result from GetWrapperClassFromActiveStarterFile
        foreach (var starterData in GetWrapperClassFromAssetConfig(tutorialModules))
        {
            //Debug.Log(starterData.Value);
            Type scriptClassType = TypeBuilder.GetType(starterData.Key);
            
            List<string> fullPath = starterData.Value.Split(new char[] {'\\', '/'}).ToList();
            var assetIndex = fullPath.IndexOf("Assets");
            var pathCategories = fullPath.Skip(assetIndex).ToArray();
            _moduleClassTypes.Add(starterData.Key, new ModuleData(scriptClassType, pathCategories));
        }

        foreach (var helperScript in CheckHelperScripts(tutorialModules))
        {
            //Debug.Log(helperScript.Value);
            Type scriptClassType = TypeBuilder.GetType(helperScript.Key);
            List<string> fullPath = helperScript.Value.Split(new char[] {'\\', '/'}).ToList();
            var assetIndex = fullPath.IndexOf("Assets");
            var pathCategories = fullPath.Skip(assetIndex).ToArray();
            _moduleClassTypes.Add(helperScript.Key, new ModuleData(scriptClassType, pathCategories));

        }

        
    }
    
    private ModuleModel IsStarterActive(TutorialModuleData moduleData)
    {
        var moduleModel = new ModuleModel();

        if (moduleData.isStarterActive)
        {
            moduleModel.mainPrefab = moduleData.starterMenuUIPrefab;
            moduleModel.hasAdditionalPrefab = moduleData.additionalPrefab;
            moduleModel.additionalPrefab = moduleData.starterAdditionalMenuUIPrefabs;
            moduleModel.type = moduleData.type;
            moduleModel.isActive = moduleData.isActive;
            moduleModel.isStarterActive = true;
            moduleModel.autoGeneratedButtonModels = moduleData.autoGeneratedButtonModelStarter;
            moduleModel.isUIDisable = moduleData.disableUI;
        }
        else
        {
            moduleModel.mainPrefab = moduleData.defaultMenuUIPrefab;
            moduleModel.hasAdditionalPrefab = moduleData.additionalPrefab;
            moduleModel.additionalPrefab = moduleData.defaultAdditionalMenuUIPrefabs;
            moduleModel.type = moduleData.type;
            moduleModel.isActive = moduleData.isActive;
            moduleModel.isStarterActive = false;
            moduleModel.autoGeneratedButtonModels = moduleData.autoGeneratedButtonModel;
            moduleModel.isUIDisable = moduleData.disableUI;

        }

        return moduleModel;
    }

    private Dictionary<string, string> GetWrapperClassFromAssetConfig(Dictionary<TutorialType, TutorialModuleData> tutorialModules)
    {
        var result = new Dictionary<string, string>();

        foreach (var moduleData in tutorialModules.Values.Where(moduleData => moduleData.isActive))
        {
            if (!moduleData.disableUI)
            {
                // make sure that ui and wrapper are not empty
                if (!moduleData.defaultModuleScript 
                    || !moduleData.starterScript 
                    || !moduleData.defaultMenuUIPrefab 
                    || !moduleData.starterMenuUIPrefab
                   )
                {
                    Debug.Log($"{moduleData.type.ToString()} contain null");
                    continue;
                }
            }
            
            if (moduleData.isStarterActive)
            {
                //Get starter wrapper from asset config
                var assetScript = $"{moduleData.starterScript.name}.cs";
                var scriptPath = assetScript;
                
#if UNITY_EDITOR && !UNITY_WEBGL
                var asset = AssetDatabase.FindAssets($"{moduleData.starterScript.name}").FirstOrDefault();
                scriptPath = AssetDatabase.GUIDToAssetPath(asset);
#endif

                Debug.Log(scriptPath);
                result.Add(moduleData.starterScript.name, scriptPath);
            }
            else
            {
                //Get default wrapper from asset config
                var assetScript = $"{moduleData.defaultModuleScript.name}.cs";
                var scriptPath = assetScript;
#if UNITY_EDITOR && !UNITY_WEBGL
                var asset = AssetDatabase.FindAssets($"{moduleData.defaultModuleScript.name}").FirstOrDefault();
                scriptPath = AssetDatabase.GUIDToAssetPath(asset);
#endif
                result.Add(moduleData.defaultModuleScript.name, scriptPath);
            }
        }

        return result;
    }

    private static Dictionary<string, string> CheckHelperScripts(Dictionary<TutorialType, TutorialModuleData> tutorialModules)
    {
        var result = new Dictionary<string, string>();
        foreach (var module in tutorialModules.Values.Where(module => module.additionalScripts && module.isActive))
        {
            if (module.isStarterActive)
            {
                module.starterHelperScripts.ToList().ForEach(x =>
                {
                    var assetScript = $"{x.name}.cs";
                    var assetPath = assetScript;
#if UNITY_EDITOR && !UNITY_WEBGL
                    var asset = AssetDatabase.FindAssets($"{x.name}").FirstOrDefault();
                    assetPath = AssetDatabase.GUIDToAssetPath(asset);
#endif
                    result.TryAdd(x.name, assetPath);
                });
            }
            else
            {
                module.defaultHelperScripts.ToList().ForEach(x =>
                {
                    var assetScript = $"{x.name}.cs";
                    var assetPath = assetScript;
#if UNITY_EDITOR && !UNITY_WEBGL
                    var asset = AssetDatabase.FindAssets($"{x.name}").FirstOrDefault();
                    assetPath = AssetDatabase.GUIDToAssetPath(asset);
#endif
                    result.TryAdd(x.name, assetPath);
                });
            }
        }

        return result;
    }
    
    /// <summary>
    /// Add all modules loaded as TutorialModuleManager GameObject's components
    /// </summary>
    private void AddModuleComponents()
    {
        foreach (ModuleData module in _moduleClassTypes.Values)
        {
            GameObject moduleGameObject = this.gameObject;
            Transform childTransform = null;
            bool objectCreated = false;
            for (int index = module.categoryPath.Length - 1; index >= 0; index--)
            {
                Transform foundTransform = this.transform.Find(module.categoryPath[index]);
                if (foundTransform != null)
                {
                    objectCreated = true;
                }
                else
                {
                    foundTransform = new GameObject(module.categoryPath[index]).transform;
                }
                
                // check if it's the desired module gameobject
                if (index == module.categoryPath.Length - 1)
                {
                    moduleGameObject = foundTransform.gameObject;
                }
                // if loops ends
                if (index == 0)
                {
                    foundTransform.SetParent(this.transform);
                    objectCreated = true;
                }
                
                // in case, child 
                if (childTransform != null)
                {
                    childTransform.SetParent(foundTransform);
                }

                // break loop if all Module Game Objects created
                if (objectCreated)
                {
                    break;
                }

                // store the newly created foundTransform for the next loop
                childTransform = foundTransform;
            }

            if (module.classType != null)
            {
                moduleGameObject.AddComponent(module.classType);
            }
        }
    }

    #endregion

    #region UI References Initialization Functions
    
    /// <summary>
    /// Prepare UI Assets that will be used from another script
    /// Need to be called where the Prefab instantiated
    /// </summary>
    public void PrepareUIAssets(GameObject viewGameObject)
    {
        List<Transform> childTransformList = new List<Transform>();
        foreach (Transform childTransform in viewGameObject.GetComponentsInChildren<Transform>())
        {
            if (childTransform != viewGameObject.transform)
            {
                childTransformList.Add(childTransform);   
            }
        }

        _moduleUITransforms.Add(viewGameObject.name, childTransformList);

        // set flag to true for later checking
        _isInstantiated = true;
    }

    /// <summary>
    /// Check function that will wait until _isInstantiated set to TRUE
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckUIInstantiated()
    {
        while (!_isInstantiated)
        {
            yield return CheckUIInstantiated();
        }
    }

    #endregion
    
    #region Getter Functions

    /// <summary>
    /// Get specific Module object to get the Module's Class.
    /// Cast the returned value to the desired class for getting the Class references.
    /// Example: <code>ClassName object = (ClassName) TutorialModuleManager.Instance.GetModuleClass("ClassName");</code>
    /// </summary>
    /// <param name="moduleId">Module's Id of the desired Module Class</param>
    /// <returns>object of the desired module component</returns>
    public Object GetModuleClass(string moduleId)
    {
        Type moduleType = _moduleClassTypes[moduleId].classType;
        return gameObject.GetComponentInChildren(moduleType);
    }
    
    public T GetModuleClass<T>()
    {
        return gameObject.GetComponentInChildren<T>();
    }
    
    /// <summary>
    /// Get specific UI Transform 
    /// </summary>
    /// <param name="prefabId">Prefab Id of the desired UI's gameobject</param>
    /// <param name="transformId">Transform Id of the desired UI gameobject in the Prefab</param>
    /// <returns>Transform of the desired UI gameobject inside the Module Prefab</returns>
    public Transform GetUITransform(string prefabId, string transformId)
    {
        IEnumerator check = CheckUIInstantiated();

        if (!check.Equals(null))
        {
            foreach (Transform uiTransform in _moduleUITransforms[prefabId])
            {
                if (uiTransform.name == transformId)
                {
                    return uiTransform;
                }
            }
        }
        
        return null;
    }

    #endregion
}
