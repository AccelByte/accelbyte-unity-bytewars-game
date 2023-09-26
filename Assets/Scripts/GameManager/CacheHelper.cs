
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class CacheHelper
{
    /// <summary>
    /// this contains cached file names, file name is retrieved from last element of uri
    /// </summary>
    private static readonly HashSet<string> CachedFiles = new HashSet<string>();

    private static bool _isCachePathLoaded;

    private const string CachePath = "cache";

    public static async void LoadTexture(string uri, int desiredWidth, int desiredHeight, Action<Texture2D> onLoadFinished)
    {
        LoadCachePath();
        Texture2D result = null;
        Debug.Log($"will load texture url:{uri}");
        var rUri = new Uri(uri);
        var fileName = GetUriLastElementFilename(rUri);
        var path = GetBaseCachePath() + fileName;
        if (CachedFiles.TryGetValue(path, out var existingPath))
        {
            var savedFile = await File.ReadAllBytesAsync(existingPath);
            result = new Texture2D(desiredWidth, desiredHeight);
            result.LoadImage(savedFile);
        }
        else
        {
            using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(uri, true);
            var w = uwr.SendWebRequest();
            while (!w.isDone)
            {
                await Task.Delay(800);
            }
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                Debug.Log($"download from {uri} successfully");
                var dht = (DownloadHandlerTexture)uwr.downloadHandler;
                await File.WriteAllBytesAsync(path, dht.data);
                CachedFiles.Add(path);
                Debug.Log($"cache saved to {path}");
                result = dht.texture;
                    
            }
        }
        onLoadFinished?.Invoke(result);
    }

    private static string _baseCachePath;
    private static void LoadCachePath()
    {
        if (!_isCachePathLoaded)
        {
            _isCachePathLoaded = true;
            string directory = GetBaseCachePath();
            try
            {
                var cacheFiles = Directory.GetFiles(directory);
                foreach (var cacheFile in cacheFiles)
                {
                    CachedFiles.Add(cacheFile);
                }
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(directory + CachePath);
                }
                Debug.Log(e.Message);
            }
        }
    }

    private static string GetBaseCachePath()
    {
        if (String.IsNullOrEmpty(_baseCachePath))
        {
            var s = Path.DirectorySeparatorChar;
            _baseCachePath = Application.persistentDataPath + s + CachePath + s;
        }
        return _baseCachePath;
    }

    private static string GetUriLastElementFilename(Uri uri)
    {
        var lastElement = uri.Segments.LastOrDefault();
        return WebUtility.UrlEncode(lastElement);
    }
}
