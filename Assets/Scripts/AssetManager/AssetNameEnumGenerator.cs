// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AssetNameEnumGenerator : AssetModificationProcessor
{
    private const string EnumPath = "/Scripts/AssetManager/AssetEnum.cs";
    private const string AssetFolder = "Assets/Resources/" + AssetManager.ModuleFolder;
    private const string MetaExtension = ".meta";
    private const string CsExtension = ".cs";

    static string[] OnWillSaveAssets(string[] paths)
    {
        List<string> newAssetNames = new();
        foreach (string path in paths)
        {
            if (path.EndsWith(MetaExtension))
            {
                continue;
            }

            if (path.EndsWith(CsExtension))
            {
                continue;
            }

            if (!path.StartsWith(AssetFolder))
            {
                continue;
            }

            newAssetNames.Add(Path.GetFileNameWithoutExtension(path));
        }

        if (newAssetNames.Count == 0) 
        {
            return paths;
        }

        UpdateAssetEnum(newAssetNames);
        return paths;
    }

    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        try
        {
            if (string.IsNullOrEmpty(Path.GetExtension(assetPath)))
            {
                Directory.Delete(assetPath, true);
                File.Delete(assetPath+MetaExtension);
            }
            else
            {
                File.Delete(assetPath);
                File.Delete(assetPath + MetaExtension);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        if (assetPath.StartsWith(AssetFolder) && !assetPath.EndsWith(CsExtension))
        {
            if (options is RemoveAssetOptions.DeleteAssets)
            {
                UpdateAssetEnum(new(){assetPath});

                return AssetDeleteResult.DidNotDelete;
            }

            UpdateAssetEnum();
        }

        return AssetDeleteResult.DidDelete;
    }

    private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
    {
        if (string.IsNullOrEmpty(Path.GetExtension(sourcePath)))
        {
            Directory.Move(sourcePath, destinationPath);
            File.Move(sourcePath+MetaExtension, destinationPath+MetaExtension);
            
            return AssetMoveResult.DidMove;
        }

        File.Move(sourcePath, destinationPath);
        File.Move(sourcePath+MetaExtension, destinationPath+MetaExtension);

        bool metaOrCsFile = sourcePath.EndsWith(MetaExtension) || sourcePath.EndsWith(CsExtension);
        bool assetFolderChanged = destinationPath.StartsWith(AssetFolder) != sourcePath.StartsWith(AssetFolder);
        bool fileInAssetFolderChanged = !Path.GetFileName(sourcePath).Equals(Path.GetFileName(destinationPath)) 
                                        && sourcePath.StartsWith(AssetFolder);

        if (!metaOrCsFile && (assetFolderChanged || fileInAssetFolderChanged))
        {
            UpdateAssetEnum();
        }
        
        return AssetMoveResult.DidMove;
    }

    private static void UpdateAssetEnum(List<string> newAssetNames = null)
    {
        List<string> enumNames = ExtractAssetNames(newAssetNames);
        enumNames.Sort();

        if (newAssetNames?.Count == 0)
        {
            return;
        }

        StringBuilder enumScript = new();
        enumScript.AppendLine(@"// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.");
        enumScript.AppendLine(@"// This is licensed software from AccelByte Inc, for limitations");
        enumScript.AppendLine(@"// and restrictions contact your company contract manager.");
        enumScript.AppendLine();
        enumScript.AppendLine(@"//Auto-generated from AssetNameEnumGenerator");
        enumScript.AppendLine(@"public enum AssetEnum");
        enumScript.AppendLine(@"{");
        foreach (string enumName in enumNames)
        {
            string formattedEnumName = enumName.Replace(" ", "_");
            enumScript.AppendLine($"\t{formattedEnumName},");
        }
        enumScript.AppendLine(@"}");

        string enumScriptString = enumScript.ToString();

        try
        {
            string existing = File.ReadAllText(Application.dataPath + EnumPath);
            bool assetUpToDate = !string.IsNullOrEmpty(existing) && enumScriptString.Equals(existing);
            if (assetUpToDate)
            {
                BytewarsLogger.Log("AssetEnum is already up to date");

                return;
            }

            using FileStream fs = File.Create(Application.dataPath + EnumPath);
            byte[] content = new UTF8Encoding(true).GetBytes(enumScriptString);
            fs.Write(content, 0, content.Length);
        }
        catch (Exception e)
        {
            BytewarsLogger.LogWarning(e.ToString());
            throw;
        }
    }

    private static List<string> ExtractAssetNames(List<string> newAssetNames = null)
    {
        List<string> enumNames = new();

        List<string> assetNames = Directory.GetFiles(AssetFolder, "*", SearchOption.AllDirectories).ToList();
        if (newAssetNames != null)
        {
            assetNames.AddRange(newAssetNames);
        }

        foreach (string file in assetNames)
        {
            string fullFileName = Path.GetFileName(file);

            bool metaOrCsFile = fullFileName.EndsWith(MetaExtension) || fullFileName.EndsWith(CsExtension);
            if (metaOrCsFile)
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(fullFileName);
            if (enumNames.Contains(fileName))
            {
                BytewarsLogger.Log($"Asset name: {fileName} already exists.");
                newAssetNames?.Remove(fileName);

                continue;
            }

            enumNames.Add(fileName);
        }

        return enumNames;
    }
}
#endif