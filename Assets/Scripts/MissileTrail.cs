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

    private float initialTime = 0f;
    private float currentAlpha = 1f;
    private float wantedAlpha = 1f;
    private float timeWithoutMissile = 0f;

    public void Init(Missile missile)
    {
        wantedAlpha = WantedAlphaStart;

        transform.SetPositionAndRotation(missile.transform.position, missile.transform.rotation);

        Color.RGBToHSV(missile.MissileState.Color, out float hue, out float saturation, out float value);
        saturation = Math.Min(1f, saturation + 0.5f);
        
        Color color = Color.HSVToRGB(hue, saturation, value);
        trailRenderer.colorGradient = new()
        {
            alphaKeys = new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f)},
            colorKeys = new GradientColorKey[] { new(color, 0.0f), new(color, 1.0f) }
        };
        trailRenderer.material.SetFloat("_Alpha", CurrentAlphaStart);

        gameObject.SetActive(true);
    }

    private void Awake()
    {
        initialTime = trailRenderer.time;
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    public void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    public void OnDisable()
    {
        Reset();
    }

    private void Update()
    {
        // When the missile is destroyed, trigger the fade out alpha animation.
        if (transform.parent == null && 
            (timeWithoutMissile += Time.deltaTime) > fadeDelayAfterMissileDestruct)
        {
            wantedAlpha = 0.0f;
        }

        // Abort if the fade out alpha animation is not triggered.
        bool isTrailOpaque = Math.Abs(wantedAlpha - WantedAlphaStart) < 0.1f;
        if (isTrailOpaque)
        {
            return;
        }
        
        // Trigger the fade out alpha animation.
        currentAlpha = Mathf.Lerp(currentAlpha, wantedAlpha, fadeRate * Time.deltaTime);
        trailRenderer.material.SetFloat("_Alpha", currentAlpha);

        // When totaly fade out, reset the missile trail.
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
        // Clear all trail dots by resetting the component and the trail lifetime.
        trailRenderer.Clear();
        trailRenderer.time = -1f;
        trailRenderer.time = initialTime;

        // Reset helper values.
        currentAlpha = CurrentAlphaStart;
        wantedAlpha = WantedAlphaStart;
        timeWithoutMissile = 0;

        // Reset game object.
        trailRenderer.material.SetFloat("_Alpha", CurrentAlphaStart);
        gameObject.SetActive(false);
    }
}
