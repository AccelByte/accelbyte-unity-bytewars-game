using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AccelByte.Core;
using AccelByte.Models;
using UnityEditor;
using UnityEngine;

public static class CreateMatchDebug
{
    private static string[] _loadedEmails;
    private static bool _isCreatingDataCanceled = false;
    private const string EmailsFilePath = "/TestData/emails.ini";
    private static readonly CreateMatchDebugTemplate[] k_Templates = new CreateMatchDebugTemplate[]
    {
        new CreateMatchDebugTemplate(InGameMode.CreateMatchEliminationGameMode, MatchSessionServerType.DedicatedServer),
        new CreateMatchDebugTemplate(InGameMode.CreateMatchEliminationGameMode, MatchSessionServerType.PeerToPeer),
        new CreateMatchDebugTemplate(InGameMode.CreateMatchDeathMatchGameMode, MatchSessionServerType.DedicatedServer),
        new CreateMatchDebugTemplate(InGameMode.CreateMatchDeathMatchGameMode, MatchSessionServerType.PeerToPeer),
    };

    private static int _templateIndex = 0;
    private static int _emailIndex = 0;
    [MenuItem("TutorialModules/Create Match Debug")]
    private static void CreateDebugCreateMatchData()
    {
        Debug.Log("uncomment and execute with caution");
        // if (ReadEmailsFromTextFile())
        // {
        //     RegisterUser();
        // }
    }

    private static bool ReadEmailsFromTextFile()
    {
        string filePath = Application.dataPath+EmailsFilePath;
        if (File.Exists(filePath))
        {
            _loadedEmails = File.ReadAllLines(filePath);
            Debug.Log($" {_loadedEmails.Length} email(s) loaded from {filePath}");
            return true;
        }
        else
        {
            Debug.LogError($"file {filePath} does not exists");
        }
        return false;
    }

    private static void RegisterUser()
    {
        if (_isCreatingDataCanceled)
        {
            Reset();
            Debug.Log("creating test data stopped");
            return;
        }
        if (_emailIndex < _loadedEmails.Length)
        {
            var email = _loadedEmails[_emailIndex];
            var userName = GetUserNameFromEmail(email);
            var dob = DateTime.Now.AddYears(-22);
            Debug.Log($"create display name: {userName}");
            MultiRegistry.GetApiClient().GetUser().Registerv2(email, 
                userName.Replace('+', '_'), GetPasswordFromUserName(userName, _emailIndex),
                userName.Replace('+', ' '), "US", dob, OnRegisteredUser);
            _emailIndex++;
        }
        else
        {
            Debug.Log("done registering user");
        }
    }

    private static void OnRegisteredUser(Result<RegisterUserResponse> result)
    {
        if (result.IsError)
        {
            Debug.LogWarning($"error creating user {result.Error.ToJsonString()}");
            RegisterUser();
        }
        else
        {
            var username = result.Value.username;
            MultiRegistry
                .GetApiClient()
                .GetUser()
                .LoginWithUsernameV3(username, GetPasswordFromUserName(username, _emailIndex),
                    OnLoginCompleted);
        }
    }

    private static void OnLoginCompleted(Result<TokenData, OAuthError> result)
    {
        if (result.IsError)
        {
            Debug.LogWarning($"error login {result.Error.ToJsonString()}");
            RegisterUser();
        }
        else
        {
            var request = GetRequest();
            request.serverName = SystemInfo.deviceName;
            MultiRegistry
                .GetApiClient()
                .GetSession()
                .CreateGameSession(request, OnCreateMatchSession);
            IncrementTemplateIndex();
        }
    }

    private static void OnCreateMatchSession(Result<SessionV2GameSession> result)
    {
        if (result.IsError)
        {
            Debug.LogWarning($"error creating match session {result.Error.ToJsonString()}");
        }
        else
        {
            Debug.Log($"success create match session");
        }
        MultiRegistry
            .GetApiClient()
            .GetUser()
            .Logout(OnLoggedOut);
    }

    private static void OnLoggedOut(Result result)
    {
        if (result.IsError)
        {
            Debug.LogWarning($"error logout {result.Error.ToJsonString()}");
        }
        else
        {
            Debug.Log($"success logged out");
        }
        RegisterUser();
    }

    private static string GetUserNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        var userName = email.Substring(0, atIndex);
        return userName;
    }

    private static string GetPasswordFromUserName(string userName, int emailIndex)
    {
        return userName+emailIndex;
    }

    private static void IncrementTemplateIndex()
    {
        if (_templateIndex == k_Templates.Length - 1)
        {
            _templateIndex = 0;
        }
        else
        {
            _templateIndex++;
        }
    }

    private static SessionV2GameSessionCreateRequest GetRequest()
    {
        if (_templateIndex < k_Templates.Length)
        {
            var template = k_Templates[_templateIndex];
            if (MatchSessionConfig.MatchRequests.TryGetValue(template.GameMode, out var serverTDict))
            {
                if (serverTDict.TryGetValue(template.MatchSessionServerType, out var request))
                {
                    return request;
                }
            }
        }
        return null;
    }
    
    [MenuItem("TutorialModules/Stop Create Match Debug")]
    private static void StopCreatingTestData()
    {
        _isCreatingDataCanceled = true;
    }

    private static void Reset()
    {
        _isCreatingDataCanceled = false;
        _emailIndex = 0;
        _templateIndex = 0;
    }
    [MenuItem("TutorialModules/Create Test Matches From Created Players")]
    private static void CreateMatchSession()
    {
        if (ReadEmailsFromTextFile())
        {
            for (int i = 0; i < _loadedEmails.Length; i++)
            {
                var email = _loadedEmails[i];
                var userName = GetUserNameFromEmail(email);
                MultiRegistry
                    .GetApiClient()
                    .GetUser()
                    .LoginWithUsernameV3(userName.Replace('+', '_'),
                        userName+i, delegate(Result<TokenData, OAuthError> loginResult)
                        {
                            if (loginResult.IsError)
                            {
                                Debug.LogWarning($"error {userName} login: {loginResult.Error.ToJsonString()}");
                            }
                            else
                            {
                                var req = GetRequest();
                                req.serverName = SystemInfo.deviceName;
                                MultiRegistry
                                    .GetApiClient()
                                    .GetSession()
                                    .CreateGameSession(req, delegate(Result<SessionV2GameSession> createMatchResult)
                                    {
                                        if (createMatchResult.IsError)
                                        {
                                            Debug.LogWarning($"error creating match: {createMatchResult.Error.ToJsonString()}");
                                        }
                                        else
                                        {
                                            Debug.Log($"success create match: {createMatchResult.Value.ToJsonString()}");
                                        }
                                    });
                                IncrementTemplateIndex();
                            }
                        } );
            }
        }
    }
    
}

public struct CreateMatchDebugTemplate
{
    public CreateMatchDebugTemplate(InGameMode gameMode, MatchSessionServerType matchSessionServerType)
    {
        GameMode = gameMode;
        MatchSessionServerType = matchSessionServerType;
    }
    public readonly InGameMode GameMode;
    public readonly MatchSessionServerType MatchSessionServerType;
}
