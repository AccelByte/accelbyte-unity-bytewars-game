// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(MeshRenderer))]
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
    private int firedMissileInt = 0;

    private float normalisedRotateSpeed = 0.0f;
    private float normalisedPowerChangeSpeed = 0.0f;
    private float missileTimer = 0.0f;

    private bool canFireMissile = true;
    private bool hasActiveMissile = false;
    private bool isShowPowerBarUI = false;

    private PowerBarUI powerBarUI;
    private PlayerState playerState;
    private Color playerColor;
    private readonly Dictionary<int, Missile> firedMissiles = new();

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

        if (!hasActiveMissile)
        {
            missileTimer += Time.deltaTime;
            if (missileTimer >= missileCooldown)
            {
                canFireMissile = true;
                missileTimer = 0.0f;
            }
        }
    }

    public void SetPlayerState(PlayerState playerState, 
        int maxMissilesInFlight, Color teamColor)
    {
        this.playerState = playerState;
        gameObject.name = $"{InGameFactory.PlayerInstancePrefix}Player{this.playerState.playerIndex + 1}";

        Init(maxMissilesInFlight, teamColor);
    }

    public void Init(int maxMissilesInFlight, Color color)
    {
        this.playerColor = color;
        this.maxMissilesInFlight = maxMissilesInFlight;

        playerInput.enabled = true;
        transform.position = playerState.position;

        if (!powerBarUI)
        {
            powerBarUI = Instantiate(powerBarUIPrefab, transform.position, Quaternion.identity, transform);
        }

        powerBarUI.Init();
        powerBarUI.SetPosition(transform.position);
        powerBarUI.SetPercentageFraction(FirePowerLevel, false);
        isShowPowerBarUI = IsShowPowerBarUI();

        SetShipColour(color);
        firedMissileInt = 0;
        gameObject.SetActive(true);
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

    public MissileFireState LocalFireMissile()
    {
        if (!canFireMissile)
        {
            return null;
        }

        canFireMissile = false;
        hasActiveMissile = true;

        List<KeyValuePair<int, Missile>> deactivatedMissiles = firedMissiles
            .Where(kvp => !kvp.Value.gameObject.activeSelf).ToList();

        foreach (KeyValuePair<int, Missile> kvp in deactivatedMissiles)
        {
            firedMissiles.Remove(kvp.Key);
        }

        if (firedMissileInt >= maxMissilesInFlight)
        {
            return null;
        }

        Vector3 missileSpawnPosition = missileSpawnPos.transform.position;
        Missile missile = GameManager.Instance.Pool.Get(missilePrefab);
        Quaternion rotation = transform.rotation;
        Vector3 velocity = transform.up.normalized * (minMissileSpeed + (maxMissileSpeed - minMissileSpeed) * FirePowerLevel);

        missile.Init(playerState, missileSpawnPosition, rotation, velocity, playerColor);
        missile.OnMissileDestroyed -= OnMissileDestroyed;

        missile.OnMissileDestroyed += OnMissileDestroyed;
        firedMissiles.Add(missile.GetId(), missile);
        firedMissileInt++;

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
        Missile missile = GameManager.Instance.Pool.Get(missilePrefab) as Missile;

        missile.SetId(missileFireState.id);
        missile.Init(playerState, missileFireState.spawnPosition, 
            missileFireState.rotation, missileFireState.velocity, missileFireState.color);

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
            LocalFireMissile();
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
        firedMissiles.Values.ToList().ForEach(missile => missile.Reset());
        firedMissiles.Clear();
        gameObject.SetActive(false);
        hasActiveMissile = false;
        canFireMissile = true;
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

    public void SyncMissile(int missileId, Vector3 velocity,
        Vector3 position, Quaternion rotation)
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

    private void OnMissileDestroyed(ulong owningPlayerNetworkClientId)
    {
        hasActiveMissile = false;

        if (playerState.clientNetworkId==owningPlayerNetworkClientId)
        {
            firedMissileInt--;
        }
    }
}
