// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class CrossPlayPreferencesWrapper : MonoBehaviour
{
    private Session sessionApi;

    private void Awake()
    {
        ApiClient apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        sessionApi = apiClient.GetSession();
    }

    public void GetPlayerSessionAttribute(ResultCallback<PlayerAttributesResponseBody> onComplete) 
    {
        sessionApi.GetPlayerAttributes((Result<PlayerAttributesResponseBody> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to get player session attribute. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.Log($"Success to get player session attribute.");
            }

            onComplete.Invoke(result);
        });
    }

    public void StorePlayerSessionAttribute(PlayerAttributesRequestBody request, ResultCallback<PlayerAttributesResponseBody> onComplete)
    {
        sessionApi.StorePlayerAttributes(request, (Result<PlayerAttributesResponseBody> result) =>
        {
            if (result.IsError)
            {
                BytewarsLogger.LogWarning($"Failed to set player session attribute. Error {result.Error.Code}: {result.Error.Message}");
            }
            else 
            {
                BytewarsLogger.Log($"Success to set player session attribute.");
            }

            onComplete.Invoke(result);
        });
    }
}
