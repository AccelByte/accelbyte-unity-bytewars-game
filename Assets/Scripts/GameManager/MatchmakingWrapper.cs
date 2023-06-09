using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MatchmakingWrapper : IMatchmaking
{
    public MatchmakingWrapper()
    {
        dummyResults = new[]
        {
            // new AgsMatchmakingResult
            // {
            //     m_errorMessage = "test failed to matchmaking on first try"
            // },

            new MatchmakingResult
            {
                m_isSuccess = true,
                m_serverIpAddress = GetLocalIPAddress(),
                m_serverPort = 7778
            }
        };
    }

    private MatchmakingResult[] dummyResults;

    private static int _resultIndex = 0;
    private static bool _isCanceled;
    public void StartMatchmaking(InGameMode inGameMode, Action<MatchmakingResult> onMatchmakingFinished)
    {
        StartMatchmakingInternal(inGameMode, onMatchmakingFinished);
    }

    private async void StartMatchmakingInternal(InGameMode inGameMode, Action<MatchmakingResult> onMatchmakingFinished)
    {
        _isCanceled = false;
        await Task.Delay(TimeSpan.FromSeconds(1));
        if(_isCanceled)
            return;
        var result = dummyResults[_resultIndex];
        result.InGameMode = inGameMode;
        #if BYTEWARS_P2P_HOST
        result.isStartAsHostP2P = true;
        #endif
        onMatchmakingFinished(result);
        _resultIndex++;
        if (_resultIndex > dummyResults.Length - 1)
        {
            _resultIndex = 0;
        }
    }

    public void CancelMatchmaking()
    {
        _isCanceled = true;
    }
    
    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        
        return "0.0.0.0";
        // return "127.0.0.1";
    }
    
}
