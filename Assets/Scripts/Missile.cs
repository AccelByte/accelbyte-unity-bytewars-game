﻿// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(ParticleSystem))]
public class Missile : GameEntityAbs
{
    private const float ScoreIncrement = 100;
    private const float SyncMissileSecond = 1;

    [Header("Missile Components"), SerializeField] private MissileExplosion missileExplosion;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private ParticleSystem missileParticleSystem;
    [SerializeField] private AudioClip travelSoundClip;
    [SerializeField] private AudioClip fireSoundClip;
    [SerializeField] private InGamePopupTextUI popupScoreTextPrefab;

    [Header("Missile Attributes"), SerializeField] private float mass = 1f;
    [SerializeField] private float radius = 0.1f;
    [SerializeField] private float skimDeltaForIncrementSeconds = 0.25f;
    [SerializeField] private float additionalSkimScoreMultiplier = 2.0f;
    [SerializeField] private float scoreIncrement = ScoreIncrement;
    [SerializeField] private float maxTimeAlive = 20.0f;
    [SerializeField] private float skimDistanceThreshold = 1.0f;
    [SerializeField] private float nearHitPlayerDistanceThreshold = 1.0f;

    private int id;
    private bool isOnServer;
    private float timeSkimmingPlanetReward = 0.0f;
    private float timeAlive = 0.0f;
    private float score = 0.0f;
    private float syncMissileTimer = 0;
    private IList<Player> nearHitPlayers = new List<Player>();
    private bool isAlive = false;

    private AudioSource travelAudioSource;
    private AudioSource fireAudioSource;

    private PlayerState owningPlayerState;
    private MissileState missileState;

    public PlayerState OwningPlayerState => owningPlayerState;
    public MissileState MissileState => missileState;

    private void Start()
    {
        List<Vector3> outerVerts = new()
        {
            new Vector3(0, 20, 0),
            new Vector3(10, 0, 0),
            new Vector3(10, -20, 0),
            new Vector3(0, -20, 0)
        };

        List<Vector3> innerVerts = new()
        {
            new Vector3(0, 10, 0),
            new Vector3(5, 0, 0),
            new Vector3(5, -15, 0),
            new Vector3(0, -15, 0)
        };

        NeonObject playerGeometry = new(outerVerts, innerVerts);

        meshFilter.mesh = new Mesh();
        Mesh mesh = meshFilter.mesh;

        mesh.Clear();
        mesh.vertices = playerGeometry.vertexList.ToArray();
        mesh.uv = playerGeometry.uvList.ToArray();
        mesh.triangles = playerGeometry.indexList.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void Init(PlayerState owningPlayerState, MissileState missileState)
    {
        this.owningPlayerState = owningPlayerState;

        missileState.Id = GetId();
        missileState.EntityId = AccelByteWarsUtility.GenerateObjectEntityId(gameObject);
        this.missileState = missileState;
        
        isAlive = true;
        isOnServer = NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer;

        transform.SetPositionAndRotation(this.missileState.SpawnPosition, this.missileState.Rotation);
        InitColor(this.missileState.Color);

        missileParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        timeSkimmingPlanetReward = 0;
        scoreIncrement = ScoreIncrement;
        score = 0;
        timeAlive = 0;

#if !UNITY_SERVER
        AddFx();
#endif

        GameManager.Instance.ActiveGEs.Add(this);
    }

    private void AddFx()
    {
        ParticleSystem.MainModule main = missileParticleSystem.main;
        main.duration = maxTimeAlive;

        missileParticleSystem.Play(false);
        if (travelAudioSource == null)
        {
            travelAudioSource = gameObject.AddComponent<AudioSource>();
            travelAudioSource.clip = travelSoundClip;
            travelAudioSource.loop = true;
        }

        if (fireAudioSource == null)
        {
            fireAudioSource = gameObject.AddComponent<AudioSource>();
            fireAudioSource.clip = fireSoundClip;
        }

        float volume = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio);
        travelAudioSource.volume = volume;
        fireAudioSource.volume = volume;
        if (volume > 0.01)
        {
            travelAudioSource.Play();
            fireAudioSource.Play();
        }
        else
        {
            travelAudioSource.Stop();
        }
    }

    private void InitColor(Color color)
    {
        if (meshRenderer)
        {
            meshRenderer.material.SetVector("_PlayerColour", color);
        }
    }

    private void Update()
    {
        UpdatePosition();
        timeAlive += Time.deltaTime;

        if (IsOutOfBound())
        {
            DestroyMissile(null);
        }

        if (timeAlive > 1.0f)
        {
            CheckSkimmingObjectScore();
            CheckNearHitPlayer();

            if (timeAlive > maxTimeAlive)
            {
                DestroyMissile(null);
            }
        }
    }

    private void UpdatePosition()
    {
        Vector3 totalForceThisFrame = GetTotalForceOnObject();
        Vector3 acceleration = totalForceThisFrame / mass;
        float deltaTime = Time.deltaTime;

        missileState.Velocity += acceleration * deltaTime;
        transform.position += missileState.Velocity * deltaTime;
        transform.rotation = Quaternion.LookRotation(missileState.Velocity, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.right);

        missileState.Position = transform.position;
        missileState.Rotation = transform.rotation;

        // Sync the missile to other players if the missile has valid owner.
        if (isOnServer && owningPlayerState != null)
        {
            syncMissileTimer += Time.deltaTime;

            if (syncMissileTimer < SyncMissileSecond)
            {
                return;
            }

            syncMissileTimer = 0;
            GameManager.Instance.MissileSyncClientRpc(owningPlayerState.ClientNetworkId,
                id, missileState.Velocity, transform.position, transform.rotation);
        }
    }

    public void Sync(Vector3 serverVelocity, Vector3 position, Quaternion rotation)
    {
        missileState.Velocity = serverVelocity;
        missileState.Position = position;
        missileState.Rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }

    private void OnTriggerEnter(Collider hitCollider)
    {
        if (NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (isAlive) 
        {
            DestroyMissile(hitCollider.gameObject);
        }
    }

    private Vector3 GetTotalForceOnObject()
    {
        Vector3 totalForce = Vector3.zero;

        IEnumerable<Planet> activePlanets = GameManager.Instance.ActiveGEs.OfType<Planet>();
        foreach (GameEntityAbs activePlanet in activePlanets)
        {
            Vector3 planetPosition = activePlanet.transform.position;
            Vector3 missilePosition = transform.position;
            float distanceBetween = Vector3.Distance(missilePosition, planetPosition);

            float gravitationalFieldRadius = activePlanet.GetRadius() * 5;
            bool withinGravitationalField = distanceBetween < gravitationalFieldRadius;
            if (!withinGravitationalField)
            {
                continue;
            }

            const int forceMultiplier = 50;
            float force = forceMultiplier * mass * activePlanet.GetMass();
            Vector3 direction = (planetPosition - missilePosition).normalized;

            totalForce += 0.01f * force * direction / Mathf.Pow(distanceBetween * 100.0f, 1.5f);
        }

        return totalForce;
    }

    public void DestroyMissile(GameObject hitObject)
    {
        Reset();

#if !UNITY_SERVER
        ShowMissileExplosion(transform.position, transform.rotation, MissileState.Color);
#endif
        GameManager.Instance.OnDestroyMissile(this, hitObject);
    }

    private void ShowMissileExplosion(Vector3 position, Quaternion rotation, Vector4 color)
    {
        if (missileExplosion)
        {
            MissileExplosion explosion = GameManager.Instance.Pool.Get(missileExplosion);

            explosion.Init(position, rotation, color);
        }
    }

    private void ShowScorePopupText(Vector3 position, Vector4 color, float score)
    {
        InGamePopupTextUI popUpText = GameManager.Instance.Pool.Get(popupScoreTextPrefab);

        popUpText.Init(position, color, score.ToString());
    }

    private bool GetIsSkimmingObject()
    {
        foreach (GameEntityAbs ge in GameManager.Instance.ActiveGEs)
        {
            if (!ge || ge.gameObject == gameObject)
            {
                continue;
            }

            float distance = Vector3.Distance(ge.transform.position, gameObject.transform.position);
            float combinedRadius = GetRadius() + ge.GetRadius();

            if (distance - combinedRadius < skimDistanceThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private void CheckSkimmingObjectScore()
    {
        float newTimeSkimmingPlanetReward = timeSkimmingPlanetReward + Time.deltaTime;
        timeSkimmingPlanetReward = GetIsSkimmingObject() ? newTimeSkimmingPlanetReward : 0.0f;

        if (timeSkimmingPlanetReward > skimDeltaForIncrementSeconds)
        {
            score += scoreIncrement;
#if !UNITY_SERVER
            ShowScorePopupText(transform.position, MissileState.Color, scoreIncrement);
#endif
            timeSkimmingPlanetReward = 0.0f;
            scoreIncrement *= additionalSkimScoreMultiplier;
        }
    }

    private void CheckNearHitPlayer()
    {
        List<GameEntityAbs> playerEntities = 
            GameManager.Instance.ActiveGEs.Where(
                p => p != null && 
                p.gameObject != gameObject && 
                p.GetComponent<Player>() != null).ToList();

        // Find near hit player.
        Player nearHitPlayer = null;
        foreach (GameEntityAbs playerEntity in playerEntities) 
        {
            float distance = Vector3.Distance(playerEntity.transform.position, gameObject.transform.position);
            float combinedRadius = GetRadius() + playerEntity.GetRadius();
            if (distance - combinedRadius < nearHitPlayerDistanceThreshold)
            {
                nearHitPlayer = playerEntity.GetComponent<Player>();
                break;
            }
        }

        // Abort if no near hit player
        if (nearHitPlayer == null)
        {
            return;
        }

        // Validate near hit player. The same player is not a valid new near hit player.
        if (!nearHitPlayers.Contains(nearHitPlayer)) 
        {
            BytewarsLogger.Log($"Near hit player: {nearHitPlayer.PlayerState.PlayerId}. Missile owner: {owningPlayerState.PlayerId}");
            nearHitPlayers.Add(nearHitPlayer);
            GameManager.Instance.OnNearHitPlayer(nearHitPlayer, this);
        }
    }

    private bool IsOutOfBound()
    {
        Vector2 center = new(0, 0);
        float width = GameConstant.MaxMissileArea.x;
        float height = GameConstant.MaxMissileArea.y;

        Rect rect = new(center.x - width / 2, center.y - height / 2, width, height);
        Vector3 pos = transform.position;

        return !rect.Contains(new Vector2(pos.x, pos.y));
    }

    public float GetScore()
    {
        return score;
    }

    public override float GetScale()
    {
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

    public override void OnHitByMissile()
    {
        BytewarsLogger.Log("missile hit by missile");
    }

    public override void Reset()
    {
        isAlive = false;
        if (GameManager.Instance.InGameState == InGameState.Playing)
        {
            GameManager.Instance.ActiveGEs.Remove(this);
        }

        timeSkimmingPlanetReward = 0;
        scoreIncrement = ScoreIncrement;
        timeAlive = 0;
        gameObject.SetActive(false);
        nearHitPlayers.Clear();

#if !UNITY_SERVER
        if (travelAudioSource)
        {
            travelAudioSource.Stop();
        }
#endif
    }

    public override void SetId(int id)
    {
        this.id = id;
    }

    public override int GetId()
    {
        return id;
    }

    public void SetPlayerState(PlayerState newPlayerState)
    {
        owningPlayerState = newPlayerState;
    }
}
