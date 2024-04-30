// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class CameraMovement
{
    private static readonly Dictionary<Camera, CancellationTokenSource> CancelCameraMovements = new();

    public static void MoveCamera(Camera camera, Vector3 targetPos)
    {
        if (camera == null)
        {
            return;
        }

        camera.transform.position = targetPos;
    }

    public static async void MoveCameraLerp(Camera camera, Vector3 targetPos, float duration = 1.0f, Action onComplete = null)
    {
        if (camera == null)
        {
            return;
        }

        const float tolerance = 0.001f;
        Vector3 startPos = camera.transform.position;

        CancellationTokenSource cancelTokenSource = new();
        CancellationToken cancelToken = cancelTokenSource.Token;
        CancelCameraMovements[camera] = cancelTokenSource;

        for (float elapsedTime = 0f; elapsedTime < duration; elapsedTime += Time.unscaledDeltaTime)
        {
            if (camera == null)
            {
                break;
            }

            if (cancelToken.IsCancellationRequested)
            {
                break;
            }

            bool isCloseEnough = Vector3.SqrMagnitude(camera.transform.position - targetPos) < tolerance;
            if (isCloseEnough)
            {
                camera.transform.position = targetPos;
                onComplete?.Invoke();
                break;
            }

            float ratio = CubicEasingOut(elapsedTime / duration);
            camera.transform.position = Vector3.Lerp(startPos, targetPos, ratio);
            await Task.Yield();
        }
    }

    public static void CancelMoveCameraLerp(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        if (!CancelCameraMovements.ContainsKey(camera))
        {
            return;
        }

        CancelCameraMovements[camera].Cancel();
        CancelCameraMovements.Remove(camera);
    }

    private static float CubicEasingOut(float ratio)
    {
        float adjustedRatio = ratio - 1f;
        return adjustedRatio * adjustedRatio * adjustedRatio + 1f;
    }
}
