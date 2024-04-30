using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Planet : GameEntityAbs
{
    public float m_scale = 1.0f;
    #if !UNITY_SERVER || BYTEWARS_DEBUG
    public float m_glowPulseRate = 2.0f;
    public float m_glowPulseScale = 3.0f;
    float m_baseGlowMin = 0.8f;
    float m_baseGlowMax = 1.0f;
    float m_glowMultiplier = 1.0f;
    #endif
    [SerializeField] private float m_mass;
    [SerializeField]
    private float m_radius;

    [SerializeField] private Renderer _renderer;

    // Start is called before the first frame update
    void Start()
    {
        m_scale = m_radius * 2.0f;
        transform.localScale = Vector3.one * m_scale;
#if UNITY_SERVER && !BYTEWARS_DEBUG
        _renderer.enabled = false;
        _renderer.material = null;
#else
        m_baseGlowMin = _renderer.material.GetFloat("_GlowMin");
        m_baseGlowMax = _renderer.material.GetFloat("_GlowMax");
#endif
    }
#if !UNITY_SERVER
    void Update()
    {
        m_glowMultiplier = Mathf.Lerp(m_glowMultiplier, 1.0f, m_glowPulseRate * Time.deltaTime);        
        _renderer.material.SetFloat("_GlowMin", m_baseGlowMin * m_glowMultiplier); 
        _renderer.material.SetFloat("_GlowMax", m_baseGlowMax * m_glowMultiplier);
    }
#endif
    public override void OnHitByMissile()
    {    
        #if !UNITY_SERVER
        m_glowMultiplier = m_glowPulseScale;
        #endif
    }
    
    public override float GetScale()
    {
        return m_scale;
    }

    public override float GetRadius()
    {
        return m_radius;
    }

    public override float GetMass()
    {
        return m_mass;
    }

    public override void Reset()
    {
        gameObject.SetActive(false);
    }

    private int _id;
    public override int GetId()
    {
        return _id;
    }

    public override void SetId(int id)
    {
        _id = id;
    }
}
