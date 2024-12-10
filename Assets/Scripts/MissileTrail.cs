// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TrailRenderer))]
public class MissileTrail : FxEntity
{
    private const float CurrentAlphaStart = 1;
    private const float WantedAlphaStart = 1;

    [SerializeField] private float fadeDelayAfterMissileDestruct = 4f;
    [SerializeField] private float fadeRate = 10f;
    [SerializeField] private TrailRenderer trailRenderer;

    private float currentAlpha = 1f;
    private float wantedAlpha = 1f;
    private float timeWithoutMissile = 0f;
    private GameObject parentMissile;
    private bool parentMissileDestroyed = false;

    public void Init(GameObject missile, Vector3 pos, Quaternion rot, Color color)
    {
        transform.SetPositionAndRotation(pos, rot);

        parentMissile = missile;
        parentMissileDestroyed = false;
        wantedAlpha = WantedAlphaStart;

        trailRenderer.colorGradient = new()
        {
            alphaKeys = new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f)},
            colorKeys = new GradientColorKey[] { new(color, 0.0f), new(color, 1.0f) }
        };
        trailRenderer.material.SetFloat("_Alpha", CurrentAlphaStart);
        
        trailRenderer.Clear();

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    public void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    public void TriggerFadeOut()
    {
        wantedAlpha = 0.0f;
    }

    private void Update()
    {
        bool hasParentMissile = parentMissile && parentMissile.activeSelf;
        if (hasParentMissile && !parentMissileDestroyed)
        {
            transform.SetPositionAndRotation(parentMissile.transform.position,
                parentMissile.transform.rotation);
        }
        else
        {
            parentMissileDestroyed = true;
            timeWithoutMissile += Time.deltaTime;
            if (timeWithoutMissile > fadeDelayAfterMissileDestruct)
            {
                TriggerFadeOut();
            }
        }

        bool isTrailOpaque = Math.Abs(wantedAlpha - WantedAlphaStart) < 0.1f;
        if (isTrailOpaque)
        {
            return;
        }

        currentAlpha = Mathf.Lerp(currentAlpha, wantedAlpha, fadeRate * Time.deltaTime);
        trailRenderer.material.SetFloat("_Alpha", currentAlpha);

        float fadeOutTolerance = 0.05f;
        bool objectIsFadedOut = currentAlpha < fadeOutTolerance;
        if (objectIsFadedOut)
        {
            Reset();
        }
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        Reset();
    }

    public override void Reset()
    {
        trailRenderer.Clear();

        currentAlpha = CurrentAlphaStart;
        wantedAlpha = WantedAlphaStart;
        parentMissile = null;
        timeWithoutMissile = 0;

        trailRenderer.material.SetFloat("_Alpha", CurrentAlphaStart);
        gameObject.SetActive(false);
    }
}
