using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class ShowPopupForceEnable : EditorWindow
{
    private static ShowPopupForceEnable popUpWindow;
    private Vector2 _scrollPos;

    public static void Init()
    {
        popUpWindow?.Close();
        
        popUpWindow = CreateInstance<ShowPopupForceEnable>();
        popUpWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 300);
        popUpWindow.titleContent = new GUIContent("Override Found on TutorialModuleConfig.json");
        popUpWindow.ShowPopup();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField($"Override Found on TutorialModuleConfig.json", EditorStyles.boldLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, alwaysShowHorizontal: false, alwaysShowVertical: true);

        EditorGUILayout.LabelField($"Is Force Disable Other Modules: {TutorialModuleForceEnable.IsForceDisable}", EditorStyles.boldLabel);

        string[] modules = TutorialModuleForceEnable.ListAllModules;
        if (modules?.Length > 0)
        {
            EditorGUILayout.LabelField($"Force Enabled Modules:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{String.Join('\n', modules)}", EditorStyles.wordWrappedLabel);
        }

        string[] invalidModules = TutorialModuleForceEnable.ForcedModules.Except(TutorialModuleForceEnable.ListAllModules).ToArray();
        if (invalidModules?.Length > 0)
        {
            EditorGUILayout.LabelField($"Invalid Module Name to Enable:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{String.Join('\n', invalidModules)}", EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Ok")) this.Close();
    }
}
#endif