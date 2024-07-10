// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetManager : MonoBehaviour
{
    private const string TutorialDataSuffix = "AssetConfig";
    public const string ModuleFolder = "Modules";

    [SerializeField] private GameManager gameManagerPrefab;

    private readonly Dictionary<string, object> assets = new();
    private readonly Dictionary<string, Object> textAssets = new();
    private readonly Dictionary<TutorialType, TutorialModuleData> tutorialModules = new();

    public static AssetManager Singleton { get; private set; }
    public GameManager GameManagerPrefab => gameManagerPrefab;
    
    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        LoadAssets();
    }

    private void LoadAssets()
    {
        Object[] objects = Resources.LoadAll(ModuleFolder);

        foreach (Object obj in objects)
        {
            if (assets.ContainsKey(obj.name))
            {
                continue;
            }

            assets.Add(obj.name, obj);
        }
    }

    #region Getter Functions

    /// <summary>
    /// Get asset by AssetEnum.
    /// </summary>
    /// <param name="assetEnum"></param>
    /// <returns>Asset object</returns>
    public object GetAsset(AssetEnum assetEnum)
    {
        string assetName = assetEnum.ToString();
        
        return GetAsset(assetName);
    }

    private object GetAsset(string assetName)
    {
        return assets.TryGetValue(assetName, out object result) ? result : null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assetFolderName"></param>
    /// <returns></returns>
    public Object[] GetAssetsInFolder(string assetFolderName)
    {
        List<Object> desiredObjects = new List<Object>();

        foreach (Object assetObject in assets.Values)
        {
            string[] assetPath = Directory.GetFiles(Application.dataPath, assetObject.name + "*", SearchOption.AllDirectories);

            if (assetPath.Length > 0 && assetPath[0].Contains("\\" + assetFolderName + "\\"))
            {
                desiredObjects.Add(assetObject);
            }
        }

        return desiredObjects.ToArray();
    }
    
    /// <summary>
    /// Get all TextAssets object under Assets/Resources/Modules
    /// </summary>
    /// <returns>array of TextAssets objects</returns>
    public Object[] GetTextAssets()
    {
        if (textAssets.Count >= 0)
        {
            foreach (Object assetObject in assets.Values)
            {
                if (assetObject is TextAsset && !textAssets.ContainsKey(assetObject.name))
                {
                    textAssets.Add(assetObject.name, assetObject);
                }
            }
        }
        
        return textAssets.Values.ToArray();
    }

    public Dictionary<TutorialType, TutorialModuleData> GetTutorialModules()
    {
        IEnumerable<KeyValuePair<string, object>> tutorialGameObjects = assets
            .Where(kvp => kvp.Key.EndsWith(TutorialDataSuffix) && kvp.Value is TutorialModuleData);

        foreach (KeyValuePair<string, object> keyValuePair in tutorialGameObjects)
        {
            TutorialModuleData tmd = keyValuePair.Value as TutorialModuleData;
            if (tmd.isBaseModule)
            {
                continue;
            }

            tutorialModules.TryAdd(tmd.type, tmd);
        }

        return tutorialModules;
    }
    
    #endregion
}
