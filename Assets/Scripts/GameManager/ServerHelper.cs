// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServerHelper
{
    private const int NoTeamIndex = -1;

    public Dictionary<int, TeamState> ConnectedTeamStates => connectedTeamStates;

    public Dictionary<ulong, PlayerState> ConnectedPlayerStates => connectedPlayerStates;

    public Dictionary<string, PlayerState> DisconnectedPlayerState => disconnectedPlayerStates;

    private Dictionary<ulong, PlayerState> connectedPlayerStates = new Dictionary<ulong, PlayerState>();
    private Dictionary<int, TeamState> connectedTeamStates = new Dictionary<int, TeamState>();

    private Dictionary<string, Player> disconnectedPlayers = new Dictionary<string, Player>();
    private readonly Dictionary<string, PlayerState> disconnectedPlayerStates = new Dictionary<string, PlayerState>();

    private MonoBehaviour monoBehaviour;
    private IEnumerator countdownCoroutine;
    private int countdownTimeLeft;
    private Action<int> onCountdownUpdate;

    public void Reset()
    {
        connectedPlayerStates.Clear();
        connectedTeamStates.Clear();
    }

    public PlayerState CreateNewPlayerState(ulong clientNetworkId, GameModeSO gameMode)
    {
        int teamIndex = GetTeamAssignment(gameMode);
        if (teamIndex == NoTeamIndex)
        {
            BytewarsLogger.LogWarning($"Cannot assign new player to a team. All team is full.");
            return null;
        }

        int playerIndex = connectedPlayerStates.Count;
        string playerName = "Player " + (playerIndex + 1);
        PlayerState playerState = new PlayerState
        {
            PlayerIndex = playerIndex,
            ClientNetworkId = clientNetworkId,
            PlayerName = playerName,
            TeamIndex = teamIndex,
            Lives = gameMode.PlayerStartLives,
            SessionId = Guid.NewGuid().ToString(),
            PlayerId = ""
        };
        BytewarsLogger.Log($"Added player {playerName} teamIndex:{teamIndex} clientNetworkId:{clientNetworkId}");

        // Add new player state if not yet.
        if (!connectedPlayerStates.ContainsKey(clientNetworkId))
        {
            connectedPlayerStates.Add(clientNetworkId, playerState);
        }

        // Add new team state if not yet.
        if (!connectedTeamStates.ContainsKey(teamIndex))
        {
            connectedTeamStates.Add(teamIndex, new TeamState
            {
                teamColour = gameMode.TeamColours[teamIndex],
                teamIndex = teamIndex
            });
        }

        return playerState;
    }

    public Player AddReconnectPlayerState(string sessionId, ulong clientNetworkId, GameModeSO gameMode)
    {
        // Get the disconnected player.
        Player player = null;
        disconnectedPlayers.Remove(sessionId, out player);
        if (player == null) 
        {
            BytewarsLogger.LogWarning("Unable to add player state to reconnect the player. Player is not found in the disconnected player list.");
            return null;
        }

        // Use existing player state if any.
        PlayerState playerState;
        if (disconnectedPlayerStates.TryGetValue(sessionId, out playerState))
        {
            disconnectedPlayerStates.Remove(sessionId);

            // Since the Unity's client network id is always new, reuse the existing player state with new client network id.
            connectedPlayerStates.Remove(playerState.ClientNetworkId);
            playerState.ClientNetworkId = clientNetworkId;
            connectedPlayerStates.Add(clientNetworkId, playerState);
        }
        // Else, create a new player state.
        else
        {
            BytewarsLogger.Log("Unable to reconnect player state, player state is not found. Creating a new player state instead.");
            playerState = CreateNewPlayerState(clientNetworkId, gameMode);
        }

        // Abort if the team index is not found.
        connectedTeamStates.TryGetValue(playerState.TeamIndex, out TeamState teamState);
        if (teamState == null) 
        {
            BytewarsLogger.LogWarning($"Unable to add player state to reconnect the player. Team {playerState.TeamIndex} is not found in the connected team state.");
            return null;            
        }

        // Assign player state to the reconnected player.
        Color teamColor = teamState.teamColour;
        player.SetPlayerState(playerState, gameMode.MaxInFlightMissilesPerPlayer, teamColor);

        return player;
    }

    public void DisconnectPlayerState(ulong clientNetworkId, Player player)
    {
        if (player == null) 
        {
            BytewarsLogger.LogWarning("Unable to disconnect player state. Player is null.");
            return;
        }

        connectedPlayerStates.TryGetValue(clientNetworkId, out PlayerState playerState);
        if (playerState == null) 
        {
            BytewarsLogger.LogWarning("Unable to disconnect player state. Player state is not found.");
            return;
        }

        // Mark player as disconnected player and store it to cache for player to handle player reconnection later.
        disconnectedPlayerStates.TryAdd(playerState.SessionId, playerState);
        disconnectedPlayers.TryAdd(playerState.SessionId, player);
    }

    public void SetTeamAndPlayerState(InGameStateResult states)
    {
        connectedPlayerStates = states.PlayerStates;
        connectedTeamStates = states.TeamStates;
    }

    public void UpdatePlayerStates(TeamState[] teamStates, PlayerState[] playerStates)
    {
        connectedPlayerStates = playerStates.ToDictionary(ps => ps.ClientNetworkId, ps => ps);
        connectedTeamStates = teamStates.ToDictionary(ts => ts.teamIndex, ts => ts);
    }

    public void RemovePlayerState(ulong clientNetworkId)
    {
        if (connectedPlayerStates.Remove(clientNetworkId, out var playerState))
        {
            RemovePlayerStateDirectly(clientNetworkId, playerState.TeamIndex, false);
        }
    }

    private void RemovePlayerStateDirectly(ulong clientNetworkId, int teamIndex, bool removeTeamIfEmpty)
    {
        connectedPlayerStates.Remove(clientNetworkId);

        // Auto remove team if empty.
        if (removeTeamIfEmpty)
        {
            Dictionary<int, int> teamsMemberCount = GetTeamsMemberCount();
            int activeMember = teamsMemberCount.Keys.Contains(teamIndex) ? teamsMemberCount[teamIndex] : 0;
            if (activeMember <= 0)
            {
                connectedTeamStates.Remove(teamIndex);
            }
        }
    }

    public PlayerState GetPlayerState(ulong networkObjectId)
    {
        return connectedPlayerStates.Keys.Contains(networkObjectId) ? connectedPlayerStates[networkObjectId] : null;
    }

    public TeamState[] GetTeamStates()
    {
        return connectedTeamStates.Values.ToArray();
    }

    #region Countdown
    public void StartCoroutineCountdown(MonoBehaviour monoBehaviour, int initialTimeLeft, Action<int> onTimeUpdated)
    {
        this.monoBehaviour ??= monoBehaviour;
        if (countdownCoroutine == null)
        {
            BytewarsLogger.Log($"Start {initialTimeLeft}s countdown coroutine.");
            countdownCoroutine = Countdown(initialTimeLeft, onTimeUpdated);
            this.monoBehaviour.StartCoroutine(countdownCoroutine);
        }
    }

    private IEnumerator Countdown(int initialTimeLeft, Action<int> onTimeUpdated)
    {
        countdownTimeLeft = initialTimeLeft;
        onCountdownUpdate = onTimeUpdated;

        while (countdownTimeLeft >= 0)
        {
            if (onCountdownUpdate != null)
            {
                onCountdownUpdate(countdownTimeLeft);
            }
            yield return new WaitForSeconds(1);
            countdownTimeLeft--;
        }

        onCountdownUpdate = null;
    }

    public void CancelCountdown()
    {
        if (monoBehaviour && countdownCoroutine != null)
        {
            BytewarsLogger.Log("Countdown coroutine stopped.");
            monoBehaviour.StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = null;
        onCountdownUpdate = null;
    }
    #endregion

    #region Helpers
    public bool IsGameOver()
    {
        int remainingActiveTeamNum = 0;
        foreach (TeamState teamState in connectedTeamStates.Values)
        {
            if (GetTeamLive(teamState.teamIndex) > 0)
            {
                remainingActiveTeamNum++;
            }
        }

        return remainingActiveTeamNum <= 1;
    }

    public int GetTeamAssignment(GameModeSO gameMode)
    {
        BytewarsLogger.Log($"GetTeamAssignment called: target team count {gameMode.TeamCount}, target player per team: ${gameMode.PlayerPerTeamCount}");

        int teamIndex = NoTeamIndex;
        Dictionary<int, int> teamMemberCount = GetTeamsMemberCount();

        // Elimination game mode.
        if (gameMode.PlayerPerTeamCount == 1)
        {
            // Try to assign to an empty team.
            if (connectedTeamStates.Count >= gameMode.TeamCount)
            {
                foreach (KeyValuePair<int, int> team in teamMemberCount)
                {
                    if (team.Value <= 0)
                    {
                        teamIndex = team.Key;
                        break;
                    }
                }
            }
            // Assign to a new team.
            else
            {
                teamIndex = connectedTeamStates.Count();
            }
        }
        // Team Deathmatch game mode.
        else if (gameMode.PlayerPerTeamCount > 1)
        {
            // Try to assign to the least populated team.
            if (connectedTeamStates.Count >= gameMode.TeamCount)
            {
                int leastTeamMemberNum = gameMode.PlayerPerTeamCount;
                foreach (KeyValuePair<int, int> team in teamMemberCount)
                {
                    if (team.Value < leastTeamMemberNum)
                    {
                        leastTeamMemberNum = team.Value;
                        teamIndex = team.Key;
                    }
                }
            }
            // Assign to a new team.
            else
            {
                teamIndex = connectedTeamStates.Count();
            }
        }

        return teamIndex;
    }

    public int GetTeamLive(int teamIndex)
    {
        int result = 0;
        foreach (PlayerState playerState in connectedPlayerStates.Values)
        {
            // Only count players who currently connected to the server.
            if (playerState.TeamIndex == teamIndex && !disconnectedPlayerStates.ContainsKey(playerState.SessionId))
            {
                result += playerState.Lives;
            }
        }
        return result;
    }

    /// <summary>
    /// Get the number of team that still has active member.
    /// </summary>
    /// <returns>Return number of team that still has active member.</returns>
    public int GetActiveTeamsCount()
    {
        return GetTeamsMemberCount().Where(team => team.Value > 0).Count();
    }

    /// <summary>
    /// Get the number of team member for each teams.
    /// </summary>
    /// <returns>Return a dictionary with team index as the key and number of team member as the value.</returns>
    public Dictionary<int, int> GetTeamsMemberCount()
    {
        Dictionary<int, int> teamMemberCount = new Dictionary<int, int>();
        foreach (TeamState teamState in connectedTeamStates.Values)
        {
            teamMemberCount.Add(teamState.teamIndex, 0);
        }
        foreach (PlayerState playerState in connectedPlayerStates.Values)
        {
            int teamIndex = playerState.TeamIndex;

            // Only count players who currently connected to the server.
            if (teamMemberCount.Keys.Contains(teamIndex) && !disconnectedPlayerStates.ContainsKey(playerState.SessionId))
            {
                teamMemberCount[teamIndex]++;
            }
        }

        return teamMemberCount;
    }
    #endregion
}
