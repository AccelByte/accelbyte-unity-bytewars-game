// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class RegionPreferencesWrapper : MonoBehaviour
{
#if !UNITY_SERVER
    public event Action OnInGameLatencyUpdated;
    public event Action OnMinimumRegionCountWarning;

    public string CurrentServerRegion => GameData.ServerType == ServerType.OnlineDedicatedServer ? currentServerRegion : string.Empty;
    public int InGameLatency => inGameLatency;

    private QosManager qosApi;
    private Lobby lobbyApi;
    private Dictionary<string, RegionPreferenceInfo> regionInfos = new Dictionary<string, RegionPreferenceInfo>();

    private bool showInGameLatency = true;
    private bool isInGameLatencyRefreshEnabled = false;
    private float inGameLatencyRefreshInterval = 5.0f;
    private Coroutine inGameLatencyRefreshCoroutine = null;

    private int inGameLatency = 0;
    private string currentServerRegion = string.Empty;

    public List<RegionPreferenceInfo> GetRegionInfos() 
    {
        return regionInfos.Values.OrderBy(x => x.Latency).ToList();
    }

    public List<RegionPreferenceInfo> GetEnabledRegions() 
    {
        return GetRegionInfos().Where(x => x.Enabled).ToList();
    }

    public RegionPreferenceInfo FindRegionInfo(string regionCode) 
    {
        regionInfos.TryGetValue(regionCode, out RegionPreferenceInfo regionInfo);
        return regionInfo;
    }

    public bool ToggleEnableRegion(string regionCode)
    {
        regionInfos.TryGetValue(regionCode, out RegionPreferenceInfo regionInfo);
        if (regionInfo != null)
        {
            if (regionInfo.Enabled && GetEnabledRegions().Count <= 1)
            {
                BytewarsLogger.LogWarning("Unable to toggle region. Need at least one region to be enabled.");
                OnMinimumRegionCountWarning?.Invoke();
            }
            else
            {
                regionInfo.Enabled = !regionInfo.Enabled;
                return true;
            }
        }

        return false;
    }

    public void QueryRegionLatencies(ResultCallback<Dictionary<string, int>> resultCallback) 
    {
        qosApi.GetAllActiveServerLatencies(result => 
        {
            if (result.IsError) 
            {
                BytewarsLogger.LogWarning($"Failed to get region latency list. Error {result.Error.Code}: {result.Error.Message}");
                regionInfos.Clear();
            }
            else 
            {
                BytewarsLogger.Log("Success to refresh region latency list.");

                Dictionary<string, RegionPreferenceInfo> newRegionInfos = new Dictionary<string, RegionPreferenceInfo>();
                foreach (KeyValuePair<string, int> latency in result.Value)
                {
                    // If the region info is not exists on cache, then store a new data and mark enabled by default.
                    RegionPreferenceInfo regionInfo = FindRegionInfo(latency.Key);
                    if (regionInfo == null)
                    {
                        regionInfo = new RegionPreferenceInfo() { Enabled = true };
                    }

                    // Update the region info.
                    regionInfo.RegionCode = latency.Key;
                    regionInfo.Latency = latency.Value;

                    newRegionInfos.Add(latency.Key, regionInfo);
                }

                // Update region info list cache.
                regionInfos = newRegionInfos;
            }

            resultCallback?.Invoke(result);
        });
    }

    public List<SessionV2GameSession> FilterEnabledRegionGameSession(List<SessionV2GameSession> gameSessions) 
    {
        if (GetEnabledRegions().Count > 0) 
        {
            gameSessions.Where(x => 
                x.configuration.type != SessionConfigurationTemplateType.P2P).ToList().
                RemoveAll(x => x.dsInformation == null || !GetEnabledRegions().Select(x => x.RegionCode).Contains(x.dsInformation.server.region));
        }
        return gameSessions;
    }

    public bool ShouldShowInGameLatency()
    {
        if (!showInGameLatency)
        {
            return false;
        }

        return GameData.ServerType != ServerType.Offline;
    }

    public void StartInGameLatencyRefresh()
    {
        BytewarsLogger.Log("Refresh in-game latency started.");
        if (!ShouldShowInGameLatency())
        {
            BytewarsLogger.LogWarning("Unable to start in-game latency refresh. Showing in-game latency is disabled.");
            return;
        }

        isInGameLatencyRefreshEnabled = true;
        StartCoroutine(RefreshInGameLatency());
    }

    public void StopInGameLatencyRefresh()
    {
        BytewarsLogger.Log("Refresh in-game latency stopped.");

        if (!isInGameLatencyRefreshEnabled) 
        {
            return;
        }

        isInGameLatencyRefreshEnabled = false;
        if (inGameLatencyRefreshCoroutine != null)
        {
            StopCoroutine(inGameLatencyRefreshCoroutine);
        }
    }

    private void Awake()
    {
        ApiClient apiClient = AccelByteSDK.GetClientRegistry().GetApi();
        qosApi = apiClient.GetQos();
        lobbyApi = apiClient.GetLobby();

        lobbyApi.Connected += OnLobbyConnected;
        lobbyApi.SessionV2DsStatusChanged += OnSessionDsStatusChanged;
        lobbyApi.SessionV2GameSessionMemberChanged += OnSessionGameSessionMemberChanged;

        CheckInGameLatencyRefreshConfig();
    }

    private void OnDestroy()
    {
        lobbyApi.Connected -= OnLobbyConnected;
        lobbyApi.SessionV2DsStatusChanged -= OnSessionDsStatusChanged;
        lobbyApi.SessionV2GameSessionMemberChanged -= OnSessionGameSessionMemberChanged;

        StopInGameLatencyRefresh();
    }

    private IEnumerator RefreshInGameLatency()
    {
        if (!isInGameLatencyRefreshEnabled)
        {
            yield return null;
        }

        GameManager.Instance.SendPingToServer();
        inGameLatency = (int)GameManager.Instance.CurrentRoundTripTime;

        BytewarsLogger.Log($"Current in-game latency: {inGameLatency} ms");
        OnInGameLatencyUpdated?.Invoke();

        yield return new WaitForSeconds(inGameLatencyRefreshInterval);

        if (isInGameLatencyRefreshEnabled)
        {
            StartCoroutine(RefreshInGameLatency());
        }
    }

    private void UpdateCurrentServerInfo(SessionV2GameSession gameSession)
    {
        currentServerRegion = "Unknown Region";

        SessionV2DsInformation dsInfo = gameSession.dsInformation;
        if (dsInfo == null)
        {
            return;
        }

        SessionV2GameServer server = dsInfo.server;
        if (server == null)
        {
            return;
        }

        currentServerRegion = server.region;
        BytewarsLogger.Log($"Current dedicated server region: {currentServerRegion}");
    }

    private void OnLobbyConnected()
    {
        QueryRegionLatencies(null);
    }

    private void OnSessionDsStatusChanged(Result<SessionV2DsStatusUpdatedNotification> result)
    {
        if (!result.IsError) 
        {
            UpdateCurrentServerInfo(result.Value.session);
        }
    }

    private void OnSessionGameSessionMemberChanged(Result<SessionV2GameMembersChangedNotification> result)
    {
        if (!result.IsError)
        {
            UpdateCurrentServerInfo(result.Value.session);
        }
    }

    #region Debugging
    private void CheckInGameLatencyRefreshConfig() 
    {
        // Read show in-game latency config.
        const string overrideShowInGameLatencyParam = "-ShowLatency=";
        if (bool.TryParse(TutorialModuleUtil.GetLaunchParamValue(overrideShowInGameLatencyParam), out bool showInGameLatencyParamValue))
        {
            showInGameLatency = showInGameLatencyParamValue;
            BytewarsLogger.Log($"Launch param sets the override show in-game latency config to {showInGameLatencyParamValue.ToString().ToUpper()}");
        }
        else if (ConfigurationReader.Config != null)
        {
            bool configValue = ConfigurationReader.Config.inGameLatencyConfiguration.showLatency;
            showInGameLatency = configValue;
            BytewarsLogger.Log($"Config file sets the override show in-game latency config to {configValue.ToString().ToUpper()}");
        }

        // Read in-game latency refresh interval config.
        const string overrideInGameLatencyRefreshIntervalParam = "-LatencyRefreshInterval=";
        if (float.TryParse(TutorialModuleUtil.GetLaunchParamValue(overrideInGameLatencyRefreshIntervalParam), out float inGameLatencyRefreshIntervalParamValue))
        {
            inGameLatencyRefreshInterval = inGameLatencyRefreshIntervalParamValue;
            BytewarsLogger.Log($"Launch param sets the override in-game latency refresh interval config to {inGameLatencyRefreshIntervalParamValue.ToString().ToUpper()}");
        }
        else if (ConfigurationReader.Config != null)
        {
            float configValue = ConfigurationReader.Config.inGameLatencyConfiguration.latencyRefreshInterval;
            inGameLatencyRefreshInterval = configValue;
            BytewarsLogger.Log($"Config file sets the override in-game latency refresh interval config to {configValue.ToString().ToUpper()}");
        }
    }
    #endregion
#endif
}
