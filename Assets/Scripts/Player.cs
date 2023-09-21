using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : GameEntityAbs
{
    public PowerBarUI m_powerBarUIPrefab;
    PowerBarUI m_powerBarUI;
    public Missile m_missilePrefab;
    public ShipDestroyedEffect m_shipDestroyedEffectPrefab;
    public FxEntity m_missileTrailPrefab;
    public PlayerInput playerInput;
    public float m_minMissileSpeed = 1.5f;
    public float m_maxMissileSpeed = 9.0f;
    private int _maxMissilesInFlight = 2;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private float _mass;
    [SerializeField] private float _radius;

    float m_normalisedRotateSpeed = 0.0f;
    float m_normalisedPowerChangeSpeed = 0.0f;

    public float FirePowerLevel { get; private set; } = 0.5f;

    private Dictionary<int, Missile> _firedMissiles = new Dictionary<int, Missile>();
    List<MissileTrail> m_missileTrails = new List<MissileTrail>();
    
    private  PlayerState _playerState;
    private  Color _colour;

    void Start()
    {       
        List<Vector3> outerVerts = new List<Vector3>();
        outerVerts.Add(new Vector3(0, 40, 0));
        outerVerts.Add(new Vector3(40, -45, 0));
        outerVerts.Add(new Vector3(25, -55, 0));
        outerVerts.Add(new Vector3(0, -45, 0));


        List<Vector3> innerVerts = new List<Vector3>();
        innerVerts.Add(new Vector3(0, 30, 0));
        innerVerts.Add(new Vector3(31.5f, -42, 0));
        innerVerts.Add(new Vector3(24, -47, 0));
        innerVerts.Add(new Vector3(0, -37, 0));

        NeonObject playerGeometry = new NeonObject(outerVerts, innerVerts);

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.Clear();
        mesh.vertices = playerGeometry.vertexList.ToArray();
        mesh.uv = playerGeometry.uvList.ToArray();
        mesh.triangles = playerGeometry.indexList.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();    
    }

    void Update()
    {
        if (m_normalisedRotateSpeed != 0)
        {
            transform.Rotate(Vector3.forward, Time.deltaTime * m_normalisedRotateSpeed * -100.0f);
        }
        if( m_normalisedPowerChangeSpeed != 0.0f )
        {
            ChangePowerLevel(m_normalisedPowerChangeSpeed);
        }
    }

    public void SetPlayerState(PlayerState playerState, 
        int maxMissilesInFlight, 
        Color teamColor)
    {
        _playerState = playerState;
        gameObject.name = InGameFactory.PlayerInstancePrefix+"Player" + (_playerState.playerIndex + 1);;
        Init(maxMissilesInFlight, teamColor);
    }

    public PlayerState PlayerState
    {
        get { return _playerState; }
    }

    private bool isShowPowerBarUI = false;
    public void Init(int maxMissilesInFlight, Color color)
    {
        _colour = color;
        playerInput.enabled = true;
        var t = transform;
        t.position = _playerState.position;
        if (!m_powerBarUI)
        {
            m_powerBarUI = Instantiate(m_powerBarUIPrefab, t.position, Quaternion.identity, t);
        }
        m_powerBarUI.Init();
        _maxMissilesInFlight = maxMissilesInFlight;
        m_powerBarUI.SetPosition(transform.position);
        SetShipColour(_colour);
        m_powerBarUI.SetPercentageFraction(FirePowerLevel,false);
        gameObject.SetActive(true);
        isShowPowerBarUI = IsShowPowerBarUI();
        _firedMissileInt = 0;
    }

    private bool IsShowPowerBarUI()
    {
        return !NetworkManager.Singleton.IsListening ||
               (NetworkManager.Singleton.IsClient &&
                NetworkManager.Singleton.LocalClientId == _playerState.clientNetworkId);
    }

    private void SetShipColour(Color color)
    {
        _renderer.material.SetVector("_PlayerColour", color);
        m_powerBarUI.SetColour(color);
    }

    public void AddKillScore(float score)
    {
        _playerState.score += score;
        _playerState.killCount++;
    }
    public MissileFireState LocalFireMissile()
    {
        var deactivatedMissiles = _firedMissiles
            .Where(kvp => !kvp.Value.gameObject.activeSelf).ToList();
        foreach (var kvp in deactivatedMissiles)
        {
            _firedMissiles.Remove(kvp.Key);
        }
        if (_firedMissileInt >= _maxMissilesInFlight)
            return null;
        Transform t = transform;
        Vector3 missileSpawnPosition = t.position + t.up * 0.25f;
        Missile missile = GameManager.Instance.Pool.Get(m_missilePrefab) as Missile;
        Quaternion rotation = t.rotation;
        var velocity = t.up * Mathf.Lerp(m_minMissileSpeed, m_maxMissileSpeed, FirePowerLevel);
        missile.Init(_playerState, missileSpawnPosition, rotation, velocity, _colour);
        missile.OnMissileDestroyed -= OnMissileDestroyed;
        missile.OnMissileDestroyed += OnMissileDestroyed;
        _firedMissiles.Add(missile.GetId(), missile);
        _firedMissileInt++;
        #if !UNITY_SERVER
        AddMissileTrail(missile.gameObject, missileSpawnPosition);
        #endif
        return new MissileFireState()
        {
            spawnPosition = missileSpawnPosition,
            rotation = rotation,
            velocity = velocity,
            color = _colour,
            id = missile.GetId()
        };
    }
    
    public void FireMissileClient(MissileFireState missileFireState, PlayerState playerState)
    {
        var missile = GameManager.Instance.Pool.Get(m_missilePrefab) as Missile;
        missile.SetId(missileFireState.id);
        missile.Init(playerState, missileFireState.spawnPosition, 
            missileFireState.rotation, missileFireState.velocity, missileFireState.color);
        _firedMissiles.TryAdd(missile.GetId(), missile);
        AddMissileTrail(missile.gameObject,  missileFireState.spawnPosition);
    }
    private void AddMissileTrail(GameObject missileGameObject, Vector3 position)
    {
        var newTrail = GameManager.Instance.Pool.Get(m_missileTrailPrefab) as MissileTrail;
        m_missileTrails.ForEach(x => x.TriggerFadeOut());
        m_missileTrails.RemoveAll(x => !x.gameObject.activeSelf);
        newTrail.Init(missileGameObject, position, transform.rotation);
        m_missileTrails.Add(newTrail);
    }


    public override void OnHitByMissile()
    {
        _playerState.lives--;
        if (_playerState.lives <= 0)
        {
            var t = transform;
            if (NetworkManager.Singleton.IsListening)
            {
                DestroyFxClientRpc(_colour, t.position, t.rotation);
            }
            else
            {
                DestroyFx(_colour, t.position, t.rotation);
            }
        }
    }
    
    [ClientRpc]
    private void DestroyFxClientRpc(Vector4 color, Vector3 position, Quaternion rotation)
    {
        DestroyFx(color, position, rotation);
    }

    private void DestroyFx(Vector4 color, Vector3 position, Quaternion rotation)
    {
        var destroyFx = GameManager.Instance.Pool.Get(m_shipDestroyedEffectPrefab) as ShipDestroyedEffect;
        destroyFx.Init(color, position, rotation);
    }
    public void SetNormalisedRotateSpeed(float normalisedRotateSpeed)
    {
        m_normalisedRotateSpeed = normalisedRotateSpeed;
    }
    public void SetNormalisedPowerChangeSpeed(float normalisedPowerChangeSpeed)
    {
        m_normalisedPowerChangeSpeed = normalisedPowerChangeSpeed;
    }

    void ChangePowerLevel(float normalisedChangeSpeed)
    {
        FirePowerLevel = Mathf.Clamp01( FirePowerLevel + normalisedChangeSpeed * Time.deltaTime );
        if (isShowPowerBarUI)
        {
            var t = transform;
            if (m_powerBarUI.transform.position != t.position)
                m_powerBarUI.transform.position = t.position;
            m_powerBarUI.SetPercentageFraction(FirePowerLevel);
        }
    }

    public void ChangePowerLevelDirectly(float powerLevel)
    {
        FirePowerLevel = powerLevel;
        if (isShowPowerBarUI)
        {
            var t = transform;
            if (m_powerBarUI.transform.position != t.position)
                m_powerBarUI.transform.position = t.position;
            m_powerBarUI.SetPercentageFraction(FirePowerLevel);
        }
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

    void OnFire(InputValue amount)
    {
        if(GameManager.Instance.InGameState==InGameState.Playing)
            LocalFireMissile();
    }

    void OnRotateShip(InputValue amount)
    {
        var gameState = GameManager.Instance.InGameState;
        if (gameState is InGameState.GameOver or InGameState.LocalPause)
            return;
        SetNormalisedRotateSpeed(amount.Get<float>());
    }
    void OnChangePower(InputValue amount)
    {
        var gameState = GameManager.Instance.InGameState;
        if (gameState is InGameState.GameOver or InGameState.LocalPause)
            return;
        SetNormalisedPowerChangeSpeed(amount.Get<float>());
    }

    void OnOpenPauseMenu()
    {
        GameManager.Instance.TriggerPauseLocalGame();
        if (GameManager.Instance.InGameState == InGameState.LocalPause)
        {
            SetNormalisedRotateSpeed(0);
        }
    }

    public override void Reset()
    {
        m_missileTrails.ForEach(x=>x.Reset());
        m_missileTrails.Clear();
        foreach (var kvp in _firedMissiles)
        {
            kvp.Value.Reset();
        }
        _firedMissiles.Clear();
        gameObject.SetActive(false);
        
    }
    private int _id = -1;
    public override void SetId(int id)
    {
        _id = id;
    }

    public override int GetId()
    {
        return _id;
    }
    public void ExplodeMissile(int missileId, Vector3 pos, Quaternion rot)
    {
        if (_firedMissiles.TryGetValue(missileId, out var missile))
        {
            missile.Destruct(pos, rot);
        }
    }

    public void SyncMissile(int missileId, Vector3 velocity,
        Vector3 position, Quaternion rotation)
    {
        if (_firedMissiles.TryGetValue(missileId, out var missile))
        {
            missile.Sync(velocity, position, rotation);
        }
    }

    public void SetFiredMissilesID(int[] firedMissilesId)
    {
        foreach (var missileId in firedMissilesId)
        {
            foreach (var activeGe in GameManager.Instance.ActiveGEs)
            {
                if (activeGe is Missile missile && missile.GetId() == missileId)
                {
                    missile.SetPlayerState(_playerState);
                    _firedMissiles.TryAdd(missileId, missile);
                    break;
                }
            }
        }
    }

    public int[] GetFiredMissilesId()
    {
        return _firedMissiles.Keys.ToArray();
    }

    public void UpdateMissilesState()
    {
        foreach (var kvp in _firedMissiles)
        {
            var missile = kvp.Value;
            if (missile && missile.gameObject.activeSelf)
            {
                missile.SetPlayerState(_playerState);
            }
        }
    }

    private int _firedMissileInt = 0;
    private void OnMissileDestroyed(ulong owningPlayerNetworkClientId)
    {
        if(_playerState.clientNetworkId==owningPlayerNetworkClientId)
            _firedMissileInt--;
    }
}
