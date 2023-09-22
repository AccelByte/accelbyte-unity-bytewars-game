using System;
using System.IO;
using System.Runtime.CompilerServices;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class SessionEssentialsWrapper_Starter : MonoBehaviour
{
    protected Session _session;
    protected Lobby _lobby;
    
    /// <summary>
    /// This event will raised after JoinSession is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnJoinSessionCompleteEvent;
    
    /// <summary>
    /// This event will raised after GetGameSessionDetailsById is complete
    /// </summary>
    public event Action<SessionResponsePayload> OnGetSessionDetailsCompleteEvent;
    

    protected void Awake()
    {
        _session = MultiRegistry.GetApiClient().GetSession();
        _lobby = MultiRegistry.GetApiClient().GetLobby();
    }

    private void Start()
    {
        LoginHandler.onLoginCompleted += LoginToLobby;
    }

    
    /// <summary>
    /// This method will invoke OnJoinSessionCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected void JoinSession(string sessionId, [CallerFilePath] string? sourceFilePath=null)
    {
        var tutorialType = SessionUtil.GetTutorialTypeFromClass((sourceFilePath));
        _session.JoinGameSession(sessionId, result => OnJoinSessionCompleted(result, tutorialType));
    }
    
    /// <summary>
    /// This method will invoke OnGetSessionDetailsCompleteEvent and return SessionRequestPayload.
    /// You can filter it by add _tutorialType static flag from class who called this method. 
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sourceFilePath">this will capture class name who called this method, leave it empty</param>
    protected void GetGameSessionDetailsById(string sessionId, [CallerFilePath] string? sourceFilePath=null)
    {
        var tutorialType = SessionUtil.GetTutorialTypeFromClass(sourceFilePath);
        _session.GetGameSessionDetailsBySessionId(sessionId,  result => OnGetGameSessionDetailsByIdComplete(result, tutorialType));
    }

    private void LoginToLobby(TokenData tokenData)
    {
        if (!_lobby.IsConnected)
        {
            _lobby.Connect();
        }
    }

    #region Callback

    
    /// <summary>
    /// JoinSession Callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnJoinSessionCompleted(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();
        
        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully joined the game session");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error.Message}");
            response.IsError = result.IsError;
        }
        OnJoinSessionCompleteEvent?.Invoke(response);
    }
    
    
    /// <summary>
    /// GetGameSessionDetailsById callback
    /// </summary>
    /// <param name="result"></param>
    /// <param name="tutorialType"></param>
    private void OnGetGameSessionDetailsByIdComplete(Result<SessionV2GameSession> result, TutorialType? tutorialType = null)
    {
        var response = new SessionResponsePayload();

        if (!result.IsError)
        {
            BytewarsLogger.Log($"Successfully obtained the game session details");
            response.Result = result;
            response.TutorialType = tutorialType;
        }
        else
        {
            BytewarsLogger.LogWarning($"{result.Error}");
            response.IsError = result.IsError;
        }
        OnGetSessionDetailsCompleteEvent?.Invoke(response);
    }
    
    #endregion
}
