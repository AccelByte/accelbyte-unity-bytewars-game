// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(MeshRenderer), typeof(Collider))]
public class Player : GameEntityAbs
{
    [Header("Player Components")]
    [SerializeField] private Renderer _renderer;
    [SerializeField] private PowerBarUI powerBarUIPrefab;
    [SerializeField] private ShipDestroyedEffect shipDestroyedEffectPrefab;
    [SerializeField] private Missile missilePrefab;
    [SerializeField] private MissileTrail missileTrailPrefab;
    [SerializeField] private Transform missileSpawnPos;
    [SerializeField] private PlayerInput playerInput;

    [Header("Player Settings")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float missileCooldown = 1.5f;
    [SerializeField] private float minMissileSpeed = 1.5f;
    [SerializeField] private float maxMissileSpeed = 9f;

    private int id = -1;
    private int maxMissilesInFlight = 2;

    private float normalisedRotateSpeed = 0.0f;
    private float normalisedPowerChangeSpeed = 0.0f;
    private float missileTimer = 0.0f;

    private bool isShowPowerBarUI = false;

    private PowerBarUI powerBarUI;
    private PlayerState playerState;
    private Color playerColor;
    private Dictionary<int, Missile> firedMissiles = new();

    public float FirePowerLevel { get; private set; } = 0.5f;
    public PlayerState PlayerState => playerState;
    public PlayerInput PlayerInput => playerInput;

    private void Start()
    {
        List<Vector3> outerVerts = new()
        {
            new Vector3(0, 40, 0),
            new Vector3(40, -45, 0),
            new Vector3(25, -55, 0),
            new Vector3(0, -45, 0)
        };

        List<Vector3> innerVerts = new()
        {
            new Vector3(0, 30, 0),
            new Vector3(31.5f, -42, 0),
            new Vector3(24, -47, 0),
            new Vector3(0, -37, 0)
        };

        NeonObject playerGeometry = new(outerVerts, innerVerts);
        Mesh mesh = new();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.Clear();
        mesh.vertices = playerGeometry.vertexList.ToArray();
        mesh.uv = playerGeometry.uvList.ToArray();
        mesh.triangles = playerGeometry.indexList.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private void Update()
    {
        if (normalisedRotateSpeed != 0)
        {
            transform.Rotate(Vector3.forward, Time.deltaTime * normalisedRotateSpeed * -100.0f);
        }

        if (normalisedPowerChangeSpeed != 0.0f)
        {
            ChangePowerLevel(normalisedPowerChangeSpeed);
        }

        if (!IsMaxMissilesInFlightReached() && missileTimer > 0.0f)
        {
            missileTimer -= Time.deltaTime;
        }
    }

    public void SetPlayerState(
        PlayerState playerState, 
        int maxMissilesInFlight, 
        Color teamColor)
    {
        this.playerState = playerState;
        gameObject.name = $"{InGameFactory.PlayerInstancePrefix}Player{this.playerState.playerIndex + 1}";

        Initialize(maxMissilesInFlight, teamColor);
    }

    public void Initialize(int maxMissilesInFlight, Color color)
    {
        Reset();

        // Initialize properties.
        powerBarUI ??= Instantiate(powerBarUIPrefab, transform.position, Quaternion.identity, transform);
        playerColor = color;
        this.maxMissilesInFlight = maxMissilesInFlight;
        playerInput.enabled = true;
        transform.position = playerState.position;

        // Display ship and enable collider.
        SetShipColour(color);
        gameObject.SetActive(true);
        GetComponent<Collider>().enabled = true;

        // Display powerbar.
        powerBarUI.Init();
        powerBarUI.SetPosition(transform.position);
        powerBarUI.SetPercentageFraction(FirePowerLevel, false);
        isShowPowerBarUI = IsShowPowerBarUI();
    }

    public void Deinitialize(bool isResetMissile) 
    {
        // Reset missile.
        if (isResetMissile)
        {
            firedMissiles.Values.ToList().ForEach(missile => missile.Reset());
            firedMissiles.Clear();
            missileTimer = 0.0f;
        }

        // Hide ship and disable collider.
        gameObject.SetActive(false);
        GetComponent<Collider>().enabled = false;
    }

    private bool IsShowPowerBarUI()
    {
        return !NetworkManager.Singleton.IsListening ||
               (NetworkManager.Singleton.IsClient &&
                NetworkManager.Singleton.LocalClientId == playerState.clientNetworkId);
    }

    private void SetShipColour(Color color)
    {
        _renderer.material.SetVector("_PlayerColour", color);
        powerBarUI.SetColour(color);
    }

    public void AddKillScore(float score)
    {
        playerState.score += score;
        playerState.killCount++;
    }

    public MissileFireState FireLocalMissile()
    {
        // Abort if shooting missile is in cooldown.
        if (missileTimer > 0.0f)
        {
            BytewarsLogger.Log("Unable to shoot a new missile. Shooting missile is in cooldown.");
            return null;
        }

        // Abort if max num of fired missiles already reached.
        if (IsMaxMissilesInFlightReached())
        {
            BytewarsLogger.Log($"Unable to shoot a new missile. Maximum active missile of {maxMissilesInFlight} already fired.");
            return null;
        }

        Vector3 missileSpawnPosition = missileSpawnPos.transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 velocity = transform.up.normalized * (minMissileSpeed + (maxMissileSpeed - minMissileSpeed) * FirePowerLevel);

        // Fire a new missile.
        Missile missile = GameManager.Instance.Pool.Get(missilePrefab);
        missile.OnMissileDestroyed -= OnMissileDestroyed;
        missile.OnMissileDestroyed += OnMissileDestroyed;
        missile.Init(playerState, missileSpawnPosition, rotation, velocity, playerColor);
        firedMissiles.Add(missile.GetId(), missile);
        
        // Initialize shoot missile cooldown.
        missileTimer = missileCooldown;

#if !UNITY_SERVER
        AddMissileTrail(missile.gameObject, missileSpawnPosition);
#endif

        return new MissileFireState()
        {
            spawnPosition = missileSpawnPosition,
            rotation = rotation,
            velocity = velocity,
            color = playerColor,
            id = missile.GetId()
        };
    }
    
    public void FireMissileClient(MissileFireState missileFireState, PlayerState playerState)
    {
        Missile missile = GameManager.Instance.Pool.Get(missilePrefab);
        missile.SetId(missileFireState.id);
        missile.Init(playerState, missileFireState.spawnPosition, missileFireState.rotation, missileFireState.velocity, missileFireState.color);
        firedMissiles.TryAdd(missile.GetId(), missile);

        AddMissileTrail(missile.gameObject, missileFireState.spawnPosition);
    }

    private void AddMissileTrail(GameObject missileGameObject, Vector3 position)
    {
        MissileTrail missileTrail = GameManager.Instance.Pool.Get(missileTrailPrefab) as MissileTrail;

        Color.RGBToHSV(playerColor, out float H, out float S, out float V);
        S = Math.Min(1f, S + 0.5f);
        Color saturatedColor = Color.HSVToRGB(H, S, V);

        missileTrail.Init(missileGameObject, position, transform.rotation, saturatedColor);
    }

    public override void OnHitByMissile()
    {
        playerState.lives--;

        if (playerState.lives <= 0)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                DestroyFxClientRpc(playerColor, transform.position, transform.rotation);
            }
            else
            {
                DestroyFx(playerColor, transform.position, transform.rotation);
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
        var destroyFx = GameManager.Instance.Pool.Get(shipDestroyedEffectPrefab);
        destroyFx.Init(color, position, rotation);
    }

    public void SetNormalisedRotateSpeed(float normalisedRotateSpeed)
    {
        this.normalisedRotateSpeed = normalisedRotateSpeed;
    }

    public void SetNormalisedPowerChangeSpeed(float normalisedPowerChangeSpeed)
    {
        this.normalisedPowerChangeSpeed = normalisedPowerChangeSpeed;
    }

    private void ChangePowerLevel(float normalisedChangeSpeed)
    {
        FirePowerLevel = Mathf.Clamp01(FirePowerLevel + normalisedChangeSpeed * Time.deltaTime);

        if (isShowPowerBarUI)
        {
            if (powerBarUI.transform.position != transform.position)
            {
                powerBarUI.transform.position = transform.position;
            }

            powerBarUI.SetPercentageFraction(FirePowerLevel);
        }
    }

    public void ChangePowerLevelDirectly(float powerLevel)
    {
        FirePowerLevel = powerLevel;
        if (isShowPowerBarUI)
        {
            if (powerBarUI.transform.position != transform.position)
            {
                powerBarUI.transform.position = transform.position;
            }

            powerBarUI.SetPercentageFraction(FirePowerLevel);
        }
    }

    public override float GetScale()
    {
        //scale is for planet for other game entity it always 1
        return 1.0f;
    }

    public override float GetRadius()
    {
        return radius;
    }

    public override float GetMass()
    {
        return mass;
    }

    private void OnFire(InputValue amount)
    {
        if (GameManager.Instance.InGameState == InGameState.Playing)
        {
            FireLocalMissile();
        }
    }

    private void OnRotateShip(InputValue amount)
    {
        InGameState gameState = GameManager.Instance.InGameState;
        if (gameState is InGameState.GameOver or InGameState.LocalPause)
        {
            return;
        }

        SetNormalisedRotateSpeed(amount.Get<float>());
    }

    private void OnChangePower(InputValue amount)
    {
        InGameState gameState = GameManager.Instance.InGameState;
        if (gameState is InGameState.GameOver or InGameState.LocalPause)
        {
            return;
        }

        SetNormalisedPowerChangeSpeed(amount.Get<float>());
    }

    void OnOpenPauseMenu()
    {
        GameManager.Instance.InGamePause.ToggleGamePause();
        if (GameManager.Instance.InGameState == InGameState.LocalPause)
        {
            SetNormalisedRotateSpeed(0);
        }
    }

    public override void Reset()
    {
        Deinitialize(true);
    }

    public override void SetId(int id)
    {
        this.id = id;
    }

    public override int GetId()
    {
        return id;
    }

    public void ExplodeMissile(int missileId, Vector3 pos, Quaternion rot)
    {
        if (firedMissiles.TryGetValue(missileId, out var missile))
        {
            missile.Destruct(pos, rot);
        }
    }

    public void SyncMissile(int missileId, Vector3 velocity, Vector3 position, Quaternion rotation)
    {
        if (firedMissiles.TryGetValue(missileId, out var missile))
        {
            missile.Sync(velocity, position, rotation);
        }
    }

    public void SetFiredMissilesId(int[] firedMissilesId)
    {
        Dictionary<int, Missile> missiles = GameManager.Instance.ActiveGEs
            .OfType<Missile>()
            .ToDictionary(missile => missile.GetId(), missile => missile);

        foreach (var missileId in firedMissilesId)
        {
            if (missiles.TryGetValue(missileId, out var missile))
            {
                missile.SetPlayerState(playerState);
                firedMissiles.TryAdd(missileId, missile);
            }
        }
    }

    public int[] GetFiredMissilesId()
    {
        return firedMissiles.Keys.ToArray();
    }

    public void UpdateMissilesState()
    {
        IEnumerable<Missile> activeMissiles = firedMissiles.Values.Where(missile =>
            missile && missile.gameObject.activeSelf);

        foreach (Missile missile in activeMissiles)
        {
            missile.SetPlayerState(playerState);
        }
    }

    private void OnMissileDestroyed(ulong owningPlayerNetworkClientId, int missileId)
    {
        firedMissiles.Remove(missileId);
    }

    private bool IsMaxMissilesInFlightReached()
    {
        return firedMissiles.Where(kvp => kvp.Value.gameObject.activeSelf).ToList().Count >= maxMissilesInFlight;
    }
}
