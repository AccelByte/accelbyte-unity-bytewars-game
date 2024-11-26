using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

///timer is not compatible with Unity's NetCode for gameobject
// using System.Timers;

public class ServerHelper
{
    private const int NoTeamIndex = -1;

    public ServerHelper()
    {
    }
    public Dictionary<int, TeamState> ConnectedTeamStates
    {
        get { return _connectedTeamState; }
    }
    public Dictionary<ulong, PlayerState> ConnectedPlayerStates
    {
        get { return _connectedPlayerState; }
    }

    private Dictionary<ulong, PlayerState> _connectedPlayerState = new Dictionary<ulong, PlayerState>();
    private Dictionary<int, TeamState> _connectedTeamState = new Dictionary<int, TeamState>();

    public void Reset()
    {
        _connectedPlayerState.Clear();
        _connectedTeamState.Clear();
    }

    public PlayerState CreateNewPlayerState(ulong clientNetworkId, GameModeSO gameMode)
    {
        int teamIndex = GetTeamAssignment(gameMode);
        if (teamIndex == NoTeamIndex)
        {
            BytewarsLogger.LogWarning($"Cannot assign new player to a team. All team is full.");
            return null;
        }

        int playerIndex = _connectedPlayerState.Count;
        string playerName = "Player " + (playerIndex + 1);
        PlayerState playerState = new PlayerState
        {
            playerIndex = playerIndex,
            clientNetworkId = clientNetworkId,
            playerName = playerName,
            teamIndex = teamIndex,
            lives = gameMode.playerStartLives,
            sessionId = Guid.NewGuid().ToString(),
            playerId = ""
        };
        BytewarsLogger.Log($"Added player {playerName} teamIndex:{teamIndex} clientNetworkId:{clientNetworkId}");

        // Add new player state if not yet.
        if (!_connectedPlayerState.ContainsKey(clientNetworkId))
        {
            _connectedPlayerState.Add(clientNetworkId, playerState);
        }

        // Add new team state if not yet.
        if (!_connectedTeamState.ContainsKey(teamIndex))
        {
            _connectedTeamState.Add(teamIndex, new TeamState
            {
                teamColour = gameMode.teamColours[teamIndex],
                teamIndex = teamIndex
            });
        }

        return playerState;
    }
    
    public int GetTeamAssignment(GameModeSO gameMode)
    {
        BytewarsLogger.Log($"GetTeamAssignment called: target team count {gameMode.teamCount}, target player per team: ${gameMode.playerPerTeamCount}");

        int teamIndex = NoTeamIndex;
        Dictionary<int, int> teamMemberCount = GetTeamsMemberCount();

        // Elimination game mode.
        if (gameMode.playerPerTeamCount == 1)
        {
            // Try to assign to an empty team.
            if (_connectedTeamState.Count >= gameMode.teamCount)
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
                teamIndex = _connectedTeamState.Count();
            }
        }
        // Team Deathmatch game mode.
        else if (gameMode.playerPerTeamCount > 1)
        {
            // Try to assign to the least populated team.
            if (_connectedTeamState.Count >= gameMode.teamCount)
            {
                int leastTeamMemberNum = gameMode.playerPerTeamCount;
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
                teamIndex = _connectedTeamState.Count();
            }
        }

        return teamIndex;
    } 

    public Dictionary<string, PlayerState> DisconnectedPlayerState
    {
        get { return disconnectedPlayerState; }
    }

    public void RemovePlayerState(ulong clientNetworkId)
    {
        if (_connectedPlayerState.Remove(clientNetworkId, out var playerState))
        {
            RemovePlayerStateDirectly(clientNetworkId, playerState.teamIndex, false);
        }
    }

    private readonly Dictionary<string, PlayerState> disconnectedPlayerState = new Dictionary<string, PlayerState>();
    public void DisconnectPlayerState(ulong clientNetworkId, Player player)
    {
        if (_connectedPlayerState.TryGetValue(clientNetworkId, out var pstate))
        {
            disconnectedPlayerState.TryAdd(pstate.sessionId, pstate);
            if (player)
            {
                disconnectedPlayers.TryAdd(pstate.sessionId, player);
            }
            _connectedPlayerState.Remove(clientNetworkId);
        }
    }

    private void RemovePlayerStateDirectly(ulong clientNetworkId, int teamIndex, bool removeTeamIfEmpty)
    {
        _connectedPlayerState.Remove(clientNetworkId);

        // Auto remove team if empty.
        if (removeTeamIfEmpty)
        {
            Dictionary<int, int> teamsMemberCount = GetTeamsMemberCount();
            int activeMember = teamsMemberCount.Keys.Contains(teamIndex) ? teamsMemberCount[teamIndex] : 0;
            if (activeMember <= 0)
            {
                _connectedTeamState.Remove(teamIndex);
            }
        }
    }

    public void UpdatePlayerStates(TeamState[] teamStates, PlayerState[] playerStates)
    {
        _connectedPlayerState = playerStates.ToDictionary(ps => ps.clientNetworkId, ps => ps);
        _connectedTeamState = teamStates.ToDictionary(ts=>ts.teamIndex, ts=>ts);
    }

    public PlayerState GetPlayerState(ulong networkObjectId)
    {
        if (_connectedPlayerState.TryGetValue(networkObjectId, out PlayerState pState))
            return pState;
        return null;
    }

    private readonly TimeSpan oneSecond = TimeSpan.FromSeconds(1);
    private const int OneSec = 1000;

    private int countdownTimeLeft;
    private Action<int> _onCountdownUpdate;
    public void CancelCountdown()
    {
        if (_monoBehaviour && _countdownCoroutine!=null)
        {
            Debug.Log("stop coroutine");
            _monoBehaviour.StopCoroutine(_countdownCoroutine);
        }
        _countdownCoroutine = null;
        _onCountdownUpdate = null;
    }

    private readonly WaitForSeconds OneSecond = new WaitForSeconds(1);
    private IEnumerator Countdown(int initialTimeLeft, Action<int> onTimeUpdated)
    {
        countdownTimeLeft = initialTimeLeft;
        _onCountdownUpdate = onTimeUpdated;
        while (countdownTimeLeft>=0)
        {
            if (_onCountdownUpdate != null)
            {
                _onCountdownUpdate(countdownTimeLeft);
            }
            yield return OneSecond;
            countdownTimeLeft--;
        }
        _onCountdownUpdate = null;
    }

    private MonoBehaviour _monoBehaviour;
    private IEnumerator _countdownCoroutine;
    public void StartCoroutineCountdown(MonoBehaviour monoBehaviour, int initialTimeLeft, Action<int> onTimeUpdated)
    {
        if (!_monoBehaviour)
            _monoBehaviour = monoBehaviour;
        if (_countdownCoroutine == null)
        {
            _countdownCoroutine = Countdown(initialTimeLeft, onTimeUpdated);
            Debug.Log("start coroutine");
            _monoBehaviour.StartCoroutine(_countdownCoroutine);
        }
    }
    
    public TeamState[] GetTeamStates()
    {
        return _connectedTeamState.Values.ToArray();
    }

    public void SetTeamAndPlayerState(InGameStateResult states)
    {
        _connectedPlayerState = states.m_playerStates;
        _connectedTeamState = states.m_teamStates;
    }

    public bool IsGameOver()
    {
        int remainingActiveTeamNum = 0;
        foreach (KeyValuePair<int, TeamState> kvp in _connectedTeamState)
        {
            if (GetTeamLive(kvp.Value.teamIndex) > 0)
            {
                remainingActiveTeamNum++;
            }
        }

        return remainingActiveTeamNum <= 1;
    }

    public Player AddReconnectPlayerState(string sessionId, 
        ulong clientNetworkId, GameModeSO gameMode)
    {
        PlayerState playerState;
        if (disconnectedPlayerState.TryGetValue(sessionId, out playerState))
        {
            playerState.clientNetworkId = clientNetworkId;
            if (_connectedPlayerState.TryAdd(clientNetworkId, playerState))
            {
                disconnectedPlayerState.Remove(sessionId);
            }
            else
            {
                Debug.LogError("unable to reconnect player state");
                return null;
            }
        }
        else
        {
            Debug.Log("unable to reconnect existing player state, create new player state instead");
            playerState = CreateNewPlayerState(clientNetworkId, gameMode);
        }

        Player player = null;
        if (disconnectedPlayers.Remove(sessionId, out player))
        {
            var teamColor = _connectedTeamState[playerState.teamIndex].teamColour;
            player.SetPlayerState(playerState, gameMode.maxInFlightMissilesPerPlayer, teamColor);
        }
        else
        {
            Debug.LogError("cant reconnect player, maybe already reconnect?");
        }
        return player;
    }
    
    public int GetTeamLive(int teamIndex)
    {
        int result = 0;
        foreach (var kvp in _connectedPlayerState)
        {
            var playerState = kvp.Value;
            if (playerState.teamIndex == teamIndex)
            {
                result += playerState.lives;
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
        foreach (KeyValuePair<int, TeamState> teamState in _connectedTeamState)
        {
            teamMemberCount.Add(teamState.Value.teamIndex, 0);
        }
        foreach (KeyValuePair<ulong, PlayerState> playerState in _connectedPlayerState)
        {
            int teamIndex = playerState.Value.teamIndex;
            if (teamMemberCount.Keys.Contains(teamIndex))
            {
                teamMemberCount[teamIndex]++;
            }
        }

        return teamMemberCount;
    }

    private Dictionary<string, Player> disconnectedPlayers = new Dictionary<string, Player>();
}
