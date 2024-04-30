// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameCamera : MonoBehaviour
{
    [SerializeField] private float minMaxSizeMultiplier;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameManager gameManager;
    
    private readonly HashSet<Missile> activeMissiles = new();
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

    private void Update()
    {
        if (!HasActiveGameEntities())
        {
            return;
        }
        
        activeMissiles.Clear();
        GetActiveMissiles(activeMissiles);

        if (activeMissiles.Count == 0)
        {
            LerpCameraSizeToDefault();
            return;
        }

        UpdateCameraSizeByMissiles(activeMissiles);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        ResetToDefaultState();
    }

    private void ResetToDefaultState()
    {
        largestWidthToFrame = GameConstant.DefaultOrthographicSize;
        mainCamera.orthographicSize = largestWidthToFrame;
        
        cameraAspectRatio = mainCamera.aspect;
        minCameraFrameExtents.y = mainCamera.orthographicSize;
        minCameraFrameExtents.x = minCameraFrameExtents.y * cameraAspectRatio;
        maxCameraFrameExtents = minCameraFrameExtents * minMaxSizeMultiplier;
        furthestMissilePositionToFrame = minCameraFrameExtents;
    }

    #endregion Initialization and Lifecycle

    #region Camera Size Adjustment 

    private void LerpCameraSizeToDefault()
    { 
        float currentSize = mainCamera.orthographicSize;
        float defaultSize = GameConstant.DefaultOrthographicSize;

        bool isCloseEnough = Mathf.Abs(currentSize - defaultSize) < cameraLerpTolerance;
        if (isCloseEnough)
        {
            return;
        }

        float newSize = Mathf.Lerp(currentSize, defaultSize, Time.deltaTime);
        mainCamera.orthographicSize = newSize;
    }

    private void UpdateCameraSizeByMissiles(IEnumerable<Missile> activeMissiles)
    {
        furthestMissilePositionToFrame = minCameraFrameExtents;
        float extraSpace = 1.0f;

        foreach (var missile in activeMissiles)
        {
            Vector3 missilePosition = missile.transform.position;
            Vector2 positionWithExtraSpace = GetPositionWithExtraSpace(missilePosition, extraSpace);
            furthestMissilePositionToFrame = Vector2.Max(furthestMissilePositionToFrame, positionWithExtraSpace);
        }

        furthestMissilePositionToFrame = Vector2.Min(furthestMissilePositionToFrame, maxCameraFrameExtents);
        largestWidthToFrame = GetLargestWidthToFrame();
        mainCamera.orthographicSize = largestWidthToFrame;
    }

    #endregion Camera Size Adjustment

    #region Camera and Game Entity Utilities

    private bool HasActiveGameEntities() => gameManager != null && gameManager.ActiveGEs.Count > 0;
    
    private void GetActiveMissiles(HashSet<Missile> activeMissiles)
    {
        foreach (var missile in gameManager.ActiveGEs.OfType<Missile>())
        {
            if (missile.isActiveAndEnabled)
            {
                activeMissiles.Add(missile);
            }
        }
    }

    private static Vector2 GetPositionWithExtraSpace(Vector3 position, float extraSpace)
    {
        float xExtra = Mathf.Abs(position.x) + extraSpace;
        float yExtra = Mathf.Abs(position.y) + extraSpace;
        return new Vector2(xExtra, yExtra);
    }

    private float GetLargestWidthToFrame()
    {
        float largestWidthBasedOnY = furthestMissilePositionToFrame.y;
        float largestWidthBasedOnX = furthestMissilePositionToFrame.x / cameraAspectRatio;
        return Mathf.Max(largestWidthBasedOnY, largestWidthBasedOnX);
    }

    #endregion Camera Calculation
}