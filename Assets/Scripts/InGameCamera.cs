// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameCamera : MonoBehaviour
{
    [SerializeField] private float minMaxSizeMultiplier = 1.2f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameManager gameManager;
    
    private Vector2 minCameraFrameExtents = new();
    private Vector2 maxCameraFrameExtents = new();
    private Vector2 furthestMissilePositionToFrame = new();
    
    private const float cameraLerpTolerance = 0.01f;
    private float cameraAspectRatio = 16.0f / 9.0f;
    private float largestWidthToFrame = 0;

    #region Initialization and Lifecycle

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResetToDefaultState();
    }

#if !UNITY_SERVER
    private void LateUpdate()
    {
        if (!HasActiveGameEntities())
        {
            return;
        }

        if (GetActiveMissiles().Count() <= 0)
        {
            LerpCameraSizeToDefault();
            return;
        }

        UpdateCameraSizeByMissiles();
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        ResetToDefaultState();
#if !UNITY_SERVER
        if (scene.buildIndex is GameConstant.GameSceneBuildIndex)
        {
            LerpCameraSizeInitial(GameConstant.DefaultOrthographicSize - 1f);
        }
#endif
    }

    private void ResetToDefaultState()
    {
        largestWidthToFrame = GameConstant.DefaultOrthographicSize;
        cameraAspectRatio = mainCamera.aspect;

        minCameraFrameExtents.y = mainCamera.orthographicSize;
        minCameraFrameExtents.x = minCameraFrameExtents.y * cameraAspectRatio;
        maxCameraFrameExtents = minCameraFrameExtents * minMaxSizeMultiplier;
        furthestMissilePositionToFrame = minCameraFrameExtents;

#if UNITY_SERVER
        mainCamera.orthographicSize = maxCameraFrameExtents.y;
#else
        mainCamera.orthographicSize = largestWidthToFrame;
#endif
    }

    #endregion Initialization and Lifecycle

    #region Camera Size Adjustment

    private async void LerpCameraSizeInitial(float initialOrthoSize)
    {
        mainCamera.orthographicSize = initialOrthoSize;

        float currentSize = initialOrthoSize;
        float defaultSize = GameConstant.DefaultOrthographicSize;

        while (true)
        {
            bool isCloseEnough = Math.Abs(currentSize - defaultSize) < cameraLerpTolerance;
            if (isCloseEnough)
            {
                return;
            }

            float lerpSpeed = 0.02f;
            float newSize = currentSize + lerpSpeed * (defaultSize - currentSize);
            mainCamera.orthographicSize = newSize;
            currentSize = newSize;

            await Task.Yield();
        }
    }

    private async void LerpCameraSizeToDefault()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        float currentSize = mainCamera.orthographicSize;
        float defaultSize = GameConstant.DefaultOrthographicSize;

        bool isCloseEnough = Math.Abs(currentSize - defaultSize) < cameraLerpTolerance;
        if (isCloseEnough)
        {
            return;
        }
        
        float lerpSpeed = 5f;
        float newSize = currentSize + lerpSpeed * (defaultSize - currentSize) * Time.deltaTime;
        mainCamera.orthographicSize = newSize;
    }

    private void UpdateCameraSizeByMissiles()
    {
        furthestMissilePositionToFrame = minCameraFrameExtents;
        float extraSpace = 2f;

        foreach (Missile missile in GetActiveMissiles())
        {
            Vector3 missilePosition = missile.transform.position;
            Vector2 positionWithExtraSpace = GetPositionWithExtraSpace(missilePosition, extraSpace);
            furthestMissilePositionToFrame = Vector2.Max(furthestMissilePositionToFrame, positionWithExtraSpace);
        }

        bool missileWithinMinExtent = furthestMissilePositionToFrame.magnitude <= minCameraFrameExtents.magnitude;
        if (missileWithinMinExtent)
        {
            return;
        }

        float bufferZone = 2f;
        Vector2 bufferZoneExtents = minCameraFrameExtents - new Vector2(bufferZone, bufferZone);

        bool missileWithinBufferZone = furthestMissilePositionToFrame.magnitude <= bufferZoneExtents.magnitude;
        if (missileWithinBufferZone)
        {
            return;
        }

        furthestMissilePositionToFrame = Vector2.Min(furthestMissilePositionToFrame, maxCameraFrameExtents);
        largestWidthToFrame = GetLargestWidthToFrame();

        float lerpSpeed = 15f;
        mainCamera.orthographicSize = mainCamera.orthographicSize + lerpSpeed 
            * (largestWidthToFrame - mainCamera.orthographicSize) * Time.deltaTime;
    }

    #endregion Camera Size Adjustment

    #region Camera and Game Entity Utilities

    private bool HasActiveGameEntities() => gameManager != null && gameManager.ActiveGEs.Count > 0;
    
    private IEnumerable<Missile> GetActiveMissiles()
    {
        return gameManager.ActiveGEs.OfType<Missile>()
            .Where(missile => missile.isActiveAndEnabled);
    }

    private static Vector2 GetPositionWithExtraSpace(Vector3 position, float extraSpace)
    {
        float xExtra = Math.Abs(position.x) + extraSpace;
        float yExtra = Math.Abs(position.y) + extraSpace;
        return new Vector2(xExtra, yExtra);
    }

    private float GetLargestWidthToFrame()
    {
        float largestWidthBasedOnY = furthestMissilePositionToFrame.y;
        float largestWidthBasedOnX = furthestMissilePositionToFrame.x / cameraAspectRatio;
        return Math.Max(largestWidthBasedOnY, largestWidthBasedOnX);
    }

    #endregion Camera Calculation
}