// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AccelByteWarsAsyncImage : MonoBehaviour
{
    public Sprite DefaultImage { private get; set; } = null;

    [SerializeField] private Image image;
    [SerializeField] private GameObject loader;

    public void SetImageTint(Color color)
    {
        image.color = color;
    }

    public async void LoadImage(string imageUrl) 
    {
        try 
        {
            // Display loader.
            await UniTask.Yield();
            loader.SetActive(true);
            image.gameObject.SetActive(false);

            await LoadImageFromUrl(imageUrl);

            // Display image and hide loader.
            await UniTask.Yield();
            loader.SetActive(false);
            image.gameObject.SetActive(true);
        } 
        catch (System.Exception e) { }
    }

    public void ResetImage() 
    {
        image.sprite = DefaultImage;
        SetImageTint(Color.white);

        loader.SetActive(false);
        image.gameObject.SetActive(true);
    }

    public Sprite GetCurrentImage() 
    {
        return image.sprite;
    }

    private void OnEnable()
    {
        // Initialize default image sprite.
        DefaultImage ??= image.sprite;
    }

    private async UniTask LoadImageFromUrl(string imageUrl)
    {
        // Load image from URL.
        try
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    image.sprite = sprite;
                    SetImageTint(Color.white);
                }
                else
                {
                    throw new System.Exception(request.error);
                }
            }
        }
        catch (System.Exception e) 
        {
            BytewarsLogger.LogWarning($"Failed to load image from URL {imageUrl}. Error: {e.Message}");
            
            // Set back to default image.
            image.sprite = DefaultImage;
        }
    }
}
