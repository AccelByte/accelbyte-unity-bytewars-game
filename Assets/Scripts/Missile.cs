using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Missile : GameEntityAbs
{
    private const float ScoreIncrement = 100;
    [SerializeField]
    private MissileExplosion missileExplosion;
    public InGamePopupTextUI m_popupScoreTextPrefab;
    public float m_skimDeltaForIncrementSeconds = 0.25f;
    public float m_additionalSkimScoreMultiplier = 2.0f;
    public float scoreIncrement = ScoreIncrement;
    public float m_maxTimeAlive = 20.0f;
    public float m_skimDistanceThreshold = 1.0f;
    [SerializeField] private float _mass;
    [SerializeField] private float _radius;
    [SerializeField] private MeshFilter _mesh;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private AudioClip travelSoundClip;
    [SerializeField] private AudioClip fireSoundClip;
    private AudioSource _travelAudioSource;
    private AudioSource _fireAudioSource;
    private float mTimeSkimmingPlanetReward = 0.0f;
    private float mTimeAlive = 0.0f;
    private float mScore = 0.0f;
    private Vector3 velocity = Vector3.zero;
    private PlayerState mOwningPlayerState;
    private Color mColour;
    private State mState = State.ClearShip;
    private bool isOnServer;
    private bool isOnClient;
    private const float SyncMissileSecond = 1;
    private float syncMissileTimer = 0;
    private enum State
    {
        ClearShip,
        Alive,
        FlaggedForDestruction
    }
    private void Start()
    {
        List<Vector3> outerVerts = new List<Vector3>();
        outerVerts.Add(new Vector3(0, 20, 0));
        outerVerts.Add(new Vector3(10, 0, 0));
        outerVerts.Add(new Vector3(10, -20, 0));
        outerVerts.Add(new Vector3(0, -20, 0));


        List<Vector3> innerVerts = new List<Vector3>();
        innerVerts.Add(new Vector3(0, 10, 0));
        innerVerts.Add(new Vector3(5, 0, 0));
        innerVerts.Add(new Vector3(5, -15, 0));
        innerVerts.Add(new Vector3(0, -15, 0));

        NeonObject playerGeometry = new NeonObject(outerVerts, innerVerts);

        Mesh mesh = new Mesh();
        _mesh.mesh = mesh;

        mesh.Clear();
        mesh.vertices = playerGeometry.vertexList.ToArray();
        mesh.uv = playerGeometry.uvList.ToArray();
        mesh.triangles = playerGeometry.indexList.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    
    public void Init(PlayerState owningPlayerState, 
        Vector3 position, Quaternion rotation, Vector3 velocity,
        Color color)
    {
        isOnServer = NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer;
        isOnClient = NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsClient;
        mState = State.ClearShip;
        mColour = color;
        mOwningPlayerState = owningPlayerState;
        var transform1 = transform;
        transform1.position = position;
        transform1.rotation = rotation;
        InitColor(mColour);

        // Tie the particle system lifetime to missile Max Time Alive so the particle emission falloff curve lines up to when the missile dies.
        // Note: the curve is tuned so the effect dies out across the range [0.85, 1], which works well for a max lifetime of ~20 seconds but may need readjusted if that value changes.

        
        // particle system has to stop to change duration
        _particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
#if !UNITY_SERVER
        AddFx();
#endif
        mTimeSkimmingPlanetReward = 0;
        scoreIncrement = ScoreIncrement;
        mScore = 0;
        mTimeAlive = 0;
        this.velocity = velocity;
        GameManager.Instance.ActiveGEs.Add(this);
    }

    private void AddFx()
    {
        var main = _particleSystem.main;
        main.duration = m_maxTimeAlive;
        _particleSystem.Play(false);
        if (_travelAudioSource == null)
        {
            _travelAudioSource = gameObject.AddComponent<AudioSource>();
            _travelAudioSource.clip = travelSoundClip;
            _travelAudioSource.loop = true;
        }

        if (_fireAudioSource == null)
        {
            _fireAudioSource = gameObject.AddComponent<AudioSource>();
            _fireAudioSource.clip = fireSoundClip;
        }

        float volume = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio);
        _travelAudioSource.volume = volume;
        _fireAudioSource.volume = volume;
        if (volume > 0.01)
        {
            _travelAudioSource.Play();
            _fireAudioSource.Play();
        }
        else
        {
            _travelAudioSource.Stop();
        }
    }

    private void InitColor(Color color)
    {
        if(_renderer)
            _renderer.material.SetVector("_PlayerColour", color);
    }

    void Update()
    {
        UpdatePosition();
        mTimeAlive += Time.deltaTime;
        var t = transform;
        if ( mState == State.FlaggedForDestruction )
        {
            OnDestroyMissile(t.position, t.rotation);            
        }
        else if( mTimeAlive > 1.0f )
        {
            if( GetIsSkimmingObject() )
            {
                mTimeSkimmingPlanetReward += Time.deltaTime;
            }
            else
            {
                mTimeSkimmingPlanetReward = 0.0f;
            }

            if( mTimeSkimmingPlanetReward > m_skimDeltaForIncrementSeconds )
            {
                mScore += scoreIncrement;
#if !UNITY_SERVER
    ShowScorePopupText(transform.position, mColour, scoreIncrement);            
#endif
                mTimeSkimmingPlanetReward = 0.0f;
                scoreIncrement *= m_additionalSkimScoreMultiplier;
            }

            if( mTimeAlive > m_maxTimeAlive )
            {
                OnDestroyMissile(t.position, t.rotation);
            }
        }
    }
    
    void UpdatePosition()
    {
        Vector3 totalForceThisFrame = GetTotalForceOnObject();
        Vector3 acceleration = totalForceThisFrame / _mass;
        var deltaTime = Time.deltaTime;
        velocity += acceleration * deltaTime;
        transform.position += velocity * deltaTime;
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.forward) 
                             * Quaternion.AngleAxis(90.0f, Vector3.right);
        if (isOnServer)
        {
            syncMissileTimer += Time.deltaTime;
            if (syncMissileTimer >= SyncMissileSecond)
            {
                syncMissileTimer = 0;
                var t = transform;
                GameManager.Instance.MissileSyncClientRpc(mOwningPlayerState.clientNetworkId, _id, 
                    velocity, t.position, t.rotation);
            }
        }
    }

    public void Sync(Vector3 serverVelocity, Vector3 position, Quaternion rotation)
    {
        velocity = serverVelocity;
        var transform1 = transform;
        transform1.position = position;
        transform1.rotation = rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer)
        {
            char prefix = other.gameObject.name[0];
            bool willBeDestroyed = false;
            Transform t = transform;
            if (prefix.Equals(InGameFactory.PlayerInstancePrefix))
            {
                if (mState == State.ClearShip)
                {
                    mState = State.Alive;
                    return;
                }
                willBeDestroyed = true;
                var player = other.GetComponent<Player>();
                GameManager.Instance.OnObjectHit(player, this);
                GameManager.Instance
                    .MissileHitClientRpc(mOwningPlayerState.clientNetworkId, _id,-1,
                        t.position, t.rotation);
            } 
            else if (prefix.Equals(InGameFactory.PlanetInstancePrefix))
            {
                var planet = other.GetComponent<Planet>();
                planet.OnHitByMissile();
                willBeDestroyed = true;
                GameManager.Instance
                    .MissileHitClientRpc(mOwningPlayerState.clientNetworkId, _id, planet.GetId(),
                        t.position, t.rotation);
            }

            if (willBeDestroyed)
            {
                mState = State.FlaggedForDestruction;
            }
        }
    }

    Vector3 GetTotalForceOnObject()
    {
        Vector3 totalForce = Vector3.zero;

        foreach (var activeObject in GameManager.Instance.ActiveGEs)
        {
            if (activeObject is not Planet) continue;
            float force = 50.0f * _mass * activeObject.GetMass();
            var position = activeObject.transform.position;
            var missilePosition = transform.position;
            float distanceBetween = Vector3.Distance(missilePosition, position);
            force /= Mathf.Pow(distanceBetween * 100.0f, 1.5f);

            Vector3 direction = (position - missilePosition).normalized;

            totalForce += direction * (0.01f * force);
        }

        return totalForce;        
    }

    void OnDestroyMissile(Vector3 pos, Quaternion rot)
    {
        #if !UNITY_SERVER
        ShowMissileExplosion(pos, rot, mColour);
        #endif
        OnMissileDestroyed?.Invoke(mOwningPlayerState.clientNetworkId);
        Reset();
    }

    public void Destruct(Vector3 pos, Quaternion rot)
    {
       OnDestroyMissile(pos, rot);
    }

    private void ShowMissileExplosion(Vector3 position, Quaternion rotation, Vector4 color)
    {
        if(missileExplosion)
        {
            var explosion = GameManager.Instance.Pool.Get(missileExplosion) as MissileExplosion;
            var transform1 = transform;
            explosion.Init(position, rotation, color);
        }
    }

    private void ShowScorePopupText(Vector3 position, Vector4 color, float score)
    {
        var popUpText = GameManager.Instance.Pool.Get(m_popupScoreTextPrefab) as InGamePopupTextUI;
        popUpText.Init(position, color, score.ToString());
    }

    bool GetIsSkimmingObject()
    {
        foreach( var ge in GameManager.Instance.ActiveGEs)
        {
            if(ge)
            {
                if (ge.gameObject != gameObject)
                {
                    float distance = Vector3.Distance(ge.transform.position, gameObject.transform.position);
                    float combinedRadius =  GetRadius() + ge.GetRadius();

                    if (distance - combinedRadius < m_skimDistanceThreshold)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public float GetScore()
    {
        return mScore;
    }

    public PlayerState GetOwningPlayerState()
    {
        return mOwningPlayerState;
    }

    public override float GetScale()
    {
        //scale is for planet for other game entity it always 1
        return 1.0f;
    }

    public override float GetRadius()
    {
        return _radius;
    }

    public override float GetMass()
    {
        return _mass;
    }

    public override void OnHitByMissile()
    {
        Debug.Log("missile hit by missile");
    }

    public override void Reset()
    {
        mState = State.ClearShip;
        if (GameManager.Instance.InGameState == InGameState.Playing)
        {
            GameManager.Instance.ActiveGEs.Remove(this);
        }
        mTimeSkimmingPlanetReward = 0;
        scoreIncrement = ScoreIncrement;
        mTimeAlive = 0;
        gameObject.SetActive(false);
#if !UNITY_SERVER
        if (_travelAudioSource)
        {
            _travelAudioSource.Stop();
        }
#endif
    }

    private int _id;
    public override void SetId(int id)
    {
        _id = id;
    }

    public override int GetId()
    {
        return _id;
    }

    public void SetPlayerState(PlayerState newPlayerState)
    {
        mOwningPlayerState = newPlayerState;
    }
    /// <summary>
    /// event with owning player client network id parameter
    /// </summary>
    public event Action<ulong> OnMissileDestroyed;
}
