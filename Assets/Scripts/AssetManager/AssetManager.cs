using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetManager : MonoBehaviour
{
    public const string ModuleFolder = "Modules";
    public static AssetManager Singleton { get; private set; }
    private readonly Dictionary<string, object> _assets = new Dictionary<string, object>();
    private readonly Dictionary<string, Object> _textAssets = new Dictionary<string, Object>();
    private const string TutorialDataSuffix = "AssetConfig";
    public GameManager GameManagerPrefab;
    
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
        var objects = Resources.LoadAll(ModuleFolder);
        foreach (var obj in objects)
        {
            if (_assets.ContainsKey(obj.name))
            {
                //Debug.Log("already contain: "+obj.name);
                continue;
            }
            _assets.Add(obj.name, obj);
        }
        //Debug.Log("assets length: "+_assets.Count);
    }
    
    #region Getter Functions
    
    /// <summary>
    /// how to access it?
    /// <code>object obj = AssetManager.Singleton.GetAsset(AssetEnum.Grid);</code>
    /// </summary>
    /// <param name="eAssetEnum"></param>
    /// <returns></returns>
    public object GetAsset(AssetEnum eAssetEnum)
    {
        string assetName = eAssetEnum.ToString();
        if (_assets.TryGetValue(assetName, out var result))
        {
            return result;
        }
        return null;
    }

    public object GetAsset(String assetName)
    {
        if (_assets.TryGetValue(assetName, out var result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assetFolderName"></param>
    /// <returns></returns>
    public Object[] GetAssetsInFolder(string assetFolderName)
    {
        List<Object> desiredObjects = new List<Object>();

        foreach (Object assetObject in _assets.Values)
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
        if (_textAssets.Count >= 0)
        {
            foreach (Object assetObject in _assets.Values)
            {
                if (assetObject is TextAsset && !_textAssets.ContainsKey(assetObject.name))
                {
                    _textAssets.Add(assetObject.name, assetObject);
                }
            }
        }
        
        return _textAssets.Values.ToArray();
    }
    

    private Dictionary<TutorialType, TutorialModuleData> tutorialModules =
        new Dictionary<TutorialType, TutorialModuleData>();
    public Dictionary<TutorialType, TutorialModuleData> GetTutorialModules()
    {
        var tutorialGameObjects = _assets
            .Where(kvp => kvp.Key.EndsWith(TutorialDataSuffix)
                          && kvp.Value is TutorialModuleData);
        foreach (var keyValuePair in tutorialGameObjects)
        {
            var tmd = keyValuePair.Value as TutorialModuleData;
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
