using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.UI;

[CustomEditor(typeof(TutorialModuleData),true)]
public class TutorialModuleDataEditor : Editor
{
    private TutorialModuleData _tutorialModuleData;
    
    private SerializedProperty _defaultMenuUIPrefab;
    private SerializedProperty _defaultModuleScript;
    private SerializedProperty _type;
    private SerializedProperty _isActive;
    private SerializedProperty _starterScript;
    private SerializedProperty _starterMenuUIPrefab;
    private SerializedProperty _isStarterActive;

    private SerializedProperty _hasAdditionalScripts;
    private SerializedProperty _defaultHelperFiles;
    private SerializedProperty _starterHelperFiles;
    
    private SerializedProperty _moduleDependencies;
    private bool _forceEnableStatus;
    private bool _forceDisableStatus;
    private TutorialModuleData _overrideModule;
    private List<string> _entrypointOptions = new List<string>();
    private bool _isDependencyModule;

    private SerializedProperty _hasAdditionalPrefab;
    private SerializedProperty _defaultAdditionalMenuUIPrefabs;
    private SerializedProperty _starterAdditionalMenuUIPrefabs;

    private bool _defaultFold = true;
    private bool _starterFold = true;

    private int toolbarInt = 0;
    private string[] toolbarStrings = {"Default", "Starter"};

    #region Tutorial Module Generated Prefabs

    private SerializedProperty _instantiatedPrefabType;
    private SerializedProperty _instantiatedGameObjectName;
    private SerializedProperty _prefabClassType;
    private GameObject _targetDefaultParentPrefab;
    private GameObject _targetStarterParentPrefab;
    private SerializedProperty _otherTutorialModule;
    private SerializedProperty _genericParentPrefab;
    private SerializedProperty _targetParentGameObjectPath;
    private SerializedProperty _spawnOrder;
    private SerializedProperty _buttonText;
    
    private bool _generationConfigFold = false;
    private bool _editGenerationConfigDisabled = true;
    private Dictionary<string, GameObject> _defaultAdditionalMenus;
    private Dictionary<string, GameObject> _starterAdditionalMenus;
    private string[] _defaultAdditionalMenusKeys;
    private string[] _starterAdditionalMenusKeys;
    
    private const string BUTTON_PREFAB_PATH = "Assets/Prefabs/UI/MenuButton.prefab";
    private GameObject _buttonPrefab;
    
    #endregion

    private void OnEnable() 
    {
        _tutorialModuleData = (TutorialModuleData)target;
        
        // Tutorial Module Generated Prefabs
        _buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BUTTON_PREFAB_PATH);
        _defaultAdditionalMenus = _tutorialModuleData.defaultAdditionalMenuUIPrefabs.ToDictionary(menu => menu.name, menu => menu.gameObject);
        _defaultAdditionalMenusKeys = _defaultAdditionalMenus.Keys.ToArray();
        _starterAdditionalMenus = _tutorialModuleData.starterAdditionalMenuUIPrefabs.ToDictionary(menu => menu.name, menu => menu.gameObject);
        _starterAdditionalMenusKeys = _starterAdditionalMenus.Keys.ToArray();
        
        _defaultMenuUIPrefab = serializedObject.FindProperty("defaultMenuUIPrefab");
        _defaultModuleScript = serializedObject.FindProperty("defaultModuleScript");
        _type = serializedObject.FindProperty("type");
        _isActive = serializedObject.FindProperty("isActive");
        _starterScript = serializedObject.FindProperty("starterScript");
        _moduleDependencies = serializedObject.FindProperty("moduleDependencies");
        _starterMenuUIPrefab = serializedObject.FindProperty("starterMenuUIPrefab");
        _isStarterActive = serializedObject.FindProperty("isStarterActive");
        var activeObject = Selection.activeObject;
        if (activeObject != null)
        {
            _forceEnableStatus = TutorialModuleForceEnable.ForceEnableModules(Selection.activeObject.name);
            _isDependencyModule = TutorialModuleForceEnable.IsDependency(Selection.activeObject.name);
        }
        _forceDisableStatus = TutorialModuleForceEnable.IsForceDisable;

        _hasAdditionalScripts = serializedObject.FindProperty("additionalScripts");
        _defaultHelperFiles = serializedObject.FindProperty("defaultHelperScripts");
        _starterHelperFiles = serializedObject.FindProperty("starterHelperScripts");
        
        _hasAdditionalPrefab = serializedObject.FindProperty("additionalPrefab");
        _defaultAdditionalMenuUIPrefabs = serializedObject.FindProperty("defaultAdditionalMenuUIPrefabs");
        _starterAdditionalMenuUIPrefabs = serializedObject.FindProperty("starterAdditionalMenuUIPrefabs");

        // Tutorial Module Generated Prefabs
        _instantiatedPrefabType = serializedObject.FindProperty("instantiatedPrefabType");
        _instantiatedGameObjectName = serializedObject.FindProperty("instantiatedGameObjectName");
        _prefabClassType = serializedObject.FindProperty("prefabClassType");
        _targetParentGameObjectPath = serializedObject.FindProperty("targetParentGameObjectPath");
        _otherTutorialModule = serializedObject.FindProperty("otherTutorialModule");
        _spawnOrder = serializedObject.FindProperty("spawnOrder");
        _buttonText = serializedObject.FindProperty("buttonText");

        // if (_tutorialModuleData.isActive)
        // {
        //     if (!CheckGeneratedPrefabExists(_tutorialModuleData.instantiatedGameObjectName, _targetDefaultParentPrefab))
        //     {
        //         SaveGenerationConfig(_tutorialModuleData, _targetDefaultParentPrefab);
        //     }
        //
        //     if (!CheckGeneratedPrefabExists(_tutorialModuleData.instantiatedGameObjectName, _targetStarterParentPrefab))
        //     {
        //         SaveGenerationConfig(_tutorialModuleData, _targetStarterParentPrefab);
        //     }
        // }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();

        _defaultFold = EditorGUILayout.BeginFoldoutHeaderGroup(_defaultFold, "Tutorial Module");
        if (_defaultFold)
        {

            EditorGUILayout.PropertyField(_defaultMenuUIPrefab);
            EditorGUILayout.PropertyField(_defaultModuleScript);
            
            EditorGUILayout.PropertyField(_type);
            EditorGUI.BeginDisabledGroup(_forceEnableStatus || _isDependencyModule || _forceDisableStatus);
            EditorGUILayout.PropertyField(_isActive);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();


        _starterFold = EditorGUILayout.BeginFoldoutHeaderGroup(_starterFold, "Tutorial Module Starter");
        if (_starterFold)
        {
            EditorGUILayout.PropertyField(_starterMenuUIPrefab);
            EditorGUILayout.PropertyField(_starterScript);
            EditorGUILayout.PropertyField(_isStarterActive);
            EditorGUILayout.Space();

        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUI.BeginDisabledGroup(_forceEnableStatus || _isDependencyModule);
        EditorGUILayout.PropertyField(_moduleDependencies, true);
        EditorGUI.EndDisabledGroup();

        AddSeparatorLine();

        EditorGUILayout.PropertyField(_hasAdditionalScripts);
        EditorGUILayout.PropertyField(_hasAdditionalPrefab);
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

        if (toolbarInt >= 0)
        {
            switch (toolbarStrings[toolbarInt])
            {
                case "Default":
                    DefaultHelperScripts();
                    break;
                case "Starter":
                    StarterHelperScripts();
                    break;
            }
        }

        AddSeparatorLine();
        
        // Tutorial Module Generated Prefabs
        _generationConfigFold = EditorGUILayout.BeginFoldoutHeaderGroup(_generationConfigFold, "Tutorial Module Generated Prefabs");
        if (_generationConfigFold)
        {
            if (GUILayout.Button(_editGenerationConfigDisabled? "Enable Editing" : "Disable Editing"))
            {
                _editGenerationConfigDisabled = !_editGenerationConfigDisabled;
            }

            EditorGUI.BeginDisabledGroup(_editGenerationConfigDisabled);
            EditorGUILayout.PropertyField(_instantiatedPrefabType, new GUIContent("Prefab Type"));
            EditorGUILayout.PropertyField(_instantiatedGameObjectName, new GUIContent("Prefab Id"));

            PrefabObjectType chosenPrefabType = _instantiatedPrefabType.GetEnumValue<PrefabObjectType>();
            if (chosenPrefabType == PrefabObjectType.OtherTutorialModuleEntryButton)
            {
                EditorGUILayout.PropertyField(_otherTutorialModule);
            }
            
            EditorGUILayout.PropertyField(_prefabClassType, new GUIContent("Tutorial Module Prefab Class Type"));
            
            PrefabClassType chosenPrefabClassType = _prefabClassType.GetEnumValue<PrefabClassType>();
            if (chosenPrefabClassType == PrefabClassType.AssociatePrefabClass)
            {
                _tutorialModuleData.associateDefaultPrefabClass = EditorGUILayout.Popup("Default Associate Prefab Class", _tutorialModuleData.associateDefaultPrefabClass, _defaultAdditionalMenusKeys);
                _tutorialModuleData.associateStarterPrefabClass = EditorGUILayout.Popup("Starter Associate Prefab Class", _tutorialModuleData.associateStarterPrefabClass, _starterAdditionalMenusKeys);
            }
            
            // Target's Game Object Structure Path. Example: "Child/GrandChild/TargetGameObject"
            EditorGUILayout.PropertyField(_targetParentGameObjectPath, new GUIContent("Target Prefab Container Path"));
            EditorGUILayout.PropertyField(_spawnOrder);
            
            if (_instantiatedPrefabType.GetEnumName<PrefabObjectType>().Contains("Button"))
            {
                EditorGUILayout.PropertyField(_buttonText);
            }
            
            if (GUILayout.Button("Generate"))
            {
                SaveGenerationConfig();
                GeneratePrefab(_targetDefaultParentPrefab);
            }

            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DefaultHelperScripts()
    {
        if (_hasAdditionalScripts.boolValue)
        {
            EditorGUI.BeginDisabledGroup(_isStarterActive.boolValue);
            EditorGUILayout.PropertyField(_defaultHelperFiles);
            EditorGUI.EndDisabledGroup();
        }
        
        if (_hasAdditionalPrefab.boolValue)
        {
            EditorGUI.BeginDisabledGroup(_isStarterActive.boolValue);
            EditorGUILayout.PropertyField(_defaultAdditionalMenuUIPrefabs);
            EditorGUI.EndDisabledGroup();
        }
    }

    private void StarterHelperScripts()
    {
        if (_hasAdditionalScripts.boolValue)
        {
            EditorGUI.BeginDisabledGroup(!_isStarterActive.boolValue);
            EditorGUILayout.PropertyField(_starterHelperFiles);
            EditorGUI.EndDisabledGroup();
        }
        
        if (_hasAdditionalPrefab.boolValue)
        {
            EditorGUI.BeginDisabledGroup(!_isStarterActive.boolValue);
            EditorGUILayout.PropertyField(_starterAdditionalMenuUIPrefabs);
            EditorGUI.EndDisabledGroup();
        }
    }

    private void SaveGenerationConfig()
    {
        // if (_otherTutorialModule.GetEnumValue<TutorialType>() != 0)
        // {
        //     _tutorialModuleData = GetTutorialModuleData(_otherTutorialModule.GetEnumValue<TutorialType>());
        // }
        
        // Get the Default Prefabs of both Default & Starter Mode
        if (_prefabClassType.GetEnumValue<PrefabClassType>() == PrefabClassType.DefaultPrefabClass)
        {
            _targetDefaultParentPrefab = _tutorialModuleData.defaultMenuUIPrefab.gameObject;
            _targetStarterParentPrefab = _tutorialModuleData.starterMenuUIPrefab.gameObject;
        }
        
        // Get the chosen Additional(Associate) Prefabs of for both Default & Starter Mode
        if (_tutorialModuleData.defaultAdditionalMenuUIPrefabs.Length > 0)
        {
            string chosenPrefabName = _defaultAdditionalMenusKeys[_tutorialModuleData.associateDefaultPrefabClass];
            _targetDefaultParentPrefab = _defaultAdditionalMenus[chosenPrefabName];
        }
        if (_tutorialModuleData.starterAdditionalMenuUIPrefabs.Length > 0)
        {
            string chosenPrefabName = _starterAdditionalMenusKeys[_tutorialModuleData.associateStarterPrefabClass];
            _targetStarterParentPrefab = _starterAdditionalMenus[chosenPrefabName];
        }
    }
    
    /// <summary>
    /// Generate the button/widget on the desired prefab menu via scene.
    /// NOTE: `MainMenu` scene might have unsaved changes. Undo to revert the scene's meta/hash.
    /// </summary>
    private void GeneratePrefab(GameObject targetParentPrefab)
    {
        if (_tutorialModuleData.instantiatedGameObjectName == "" || !targetParentPrefab || _tutorialModuleData.targetParentGameObjectPath == "")
        {
            Debug.LogWarning($"[{_tutorialModuleData.name}] `Prefab Id`, `Prefab Class Type`, and `Target Parent Container Name` can't be empty!");
            return;
        }

        Debug.Log("[Editor] Generating prefab in progress..");
        GameObject parentInstance = PrefabUtility.InstantiatePrefab(targetParentPrefab) as GameObject;
        Transform targetParentTransform = parentInstance.transform.Find(_tutorialModuleData.targetParentGameObjectPath);
        if (targetParentTransform)
        {
            GameObject instantiatedGameObject = PrefabUtility.InstantiatePrefab(_buttonPrefab, targetParentTransform) as GameObject;
            instantiatedGameObject.name = _tutorialModuleData.instantiatedGameObjectName;
            
            TMP_Text instantiatedButton = instantiatedGameObject.GetComponentInChildren<TMP_Text>();
            instantiatedButton.text = _tutorialModuleData.buttonText;
            PrefabUtility.ApplyPrefabInstance(parentInstance, InteractionMode.UserAction);
        }

        if (parentInstance) DestroyImmediate(parentInstance);
    }
    
    private bool CheckGeneratedPrefabExists(string prefabName, GameObject targetParentPrefab)
    {
        if (prefabName == "" || !targetParentPrefab)
        {
            Debug.LogWarning($"[{_tutorialModuleData.name}] Prefab Id and Prefab Class Type related can't be empty!");
            return false;
        }
        
        if (targetParentPrefab.transform.Find(prefabName))
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Remove the generated prefab of the current configuration via scene.
    /// NOTE: `MainMenu` scene might have unsaved changes. Undo to revert the scene's meta/hash.
    /// </summary>
    private void RemoveGeneratedPrefab(GameObject targetParentPrefab)
    {
        GameObject parentInstance = PrefabUtility.InstantiatePrefab(targetParentPrefab) as GameObject;
        Transform generatedPrefab = parentInstance.transform.Find(_tutorialModuleData.instantiatedGameObjectName);
        if (generatedPrefab)
        {
            DestroyImmediate(generatedPrefab.gameObject);
            PrefabUtility.ApplyPrefabInstance(parentInstance, InteractionMode.UserAction);
        }
        if (parentInstance)
        {
            DestroyImmediate(parentInstance);
        }
    }
    
    /// <summary>
    /// Draw a line in the inspector for separating sections
    /// </summary>
    private void AddSeparatorLine()
    {
        EditorGUILayout.Space();
        Rect separatorRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(separatorRect, Color.black);
        EditorGUILayout.Space();
    }
    
    private TutorialModuleData GetTutorialModuleData(TutorialType tutorialType)
    {
        TutorialModuleData[] tutorialModuleDatas = Resources.FindObjectsOfTypeAll<TutorialModuleData>();
        foreach (TutorialModuleData tutorialModuleData in tutorialModuleDatas)
        {
            if (tutorialModuleData.name.Contains(tutorialType.ToString()))
            {
                return tutorialModuleData;
            }
        }
        return null;
    }
}
