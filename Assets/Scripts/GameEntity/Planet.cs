// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Planet : GameEntityAbs
{
    public static readonly int InvalidPlanetId = -1;

    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float mass;
    [SerializeField] private float radius;
    [SerializeField] private float scale = 1.0f;

#if !UNITY_SERVER
    [SerializeField] private float glowPulseRate = 2.0f;
    [SerializeField] private float glowPulseScale = 3.0f;
    private float baseGlowMin = 0.8f;
    private float baseGlowMax = 1.0f;
    private float glowMultiplier = 1.0f;
#endif

    private PlanetState planetState;

    public PlanetState PlanetState => planetState;

    private void OnEnable()
    {
        planetState ??= new PlanetState();
        planetState.EntityId = AccelByteWarsUtility.GenerateObjectEntityId(gameObject);
        planetState.Position = transform.position;
    }

    private void Start()
    {
        scale = radius * 2.0f;
        transform.localScale = Vector3.one * scale;
#if UNITY_SERVER && !BYTEWARS_DEBUG
        meshRenderer.enabled = false;
        meshRenderer.material = null;
#else
        baseGlowMin = meshRenderer.material.GetFloat("_GlowMin");
        baseGlowMax = meshRenderer.material.GetFloat("_GlowMax");
#endif
    }

#if !UNITY_SERVER
    private void Update()
    {
        glowMultiplier = Mathf.Lerp(glowMultiplier, 1.0f, glowPulseRate * Time.deltaTime);        
        meshRenderer.material.SetFloat("_GlowMin", baseGlowMin * glowMultiplier); 
        meshRenderer.material.SetFloat("_GlowMax", baseGlowMax * glowMultiplier);
    }
#endif

    public override void OnHitByMissile()
    {
#if !UNITY_SERVER
        glowMultiplier = glowPulseScale;
#endif
    }
    
    public override float GetScale()
    {
        return scale;
    }

    public override float GetRadius()
    {
        return radius;
    }

    public override float GetMass()
    {
        return mass;
    }

    public override void Reset()
    {
        gameObject.SetActive(false);
    }

    public override int GetId()
    {
        return planetState.Id;
    }

    public override void SetId(int id)
    {
        planetState.Id = id;
    }
}
