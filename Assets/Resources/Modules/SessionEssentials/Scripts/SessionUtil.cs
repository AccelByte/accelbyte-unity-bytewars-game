// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AccelByte.Models;
using UnityEngine;

public static class SessionUtil
{
        public static SessionV2GameSessionCreateRequest CreateGameSessionRequest(SessionRequestPayload sessionRequest)
        {
                var type = sessionRequest.SessionType;
                var gameSessionRequest = new SessionV2GameSessionCreateRequest
                {
                        type = GetSessionConfigTemplate(type),
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = !String.IsNullOrEmpty(sessionRequest.SessionTemplateName) ? sessionRequest.SessionTemplateName : sessionRequest.MatchPool,
                        matchPool = sessionRequest.MatchPool,
                        attributes = sessionRequest.attributes
                };
                return gameSessionRequest;
        }

        private static SessionConfigurationTemplateType GetSessionConfigTemplate(SessionType sessionType)
        {
                switch (sessionType)
                {
                        case SessionType.none:
                                return SessionConfigurationTemplateType.NONE;
                        case SessionType.dedicated:
                                return SessionConfigurationTemplateType.DS;
                        case SessionType.p2p:
                                return SessionConfigurationTemplateType.P2P;
                        default:
                                Debug.LogWarning($"");
                                break;
                }
                return SessionConfigurationTemplateType.NONE;
        }

        public static TutorialType? GetTutorialTypeFromClass(string filePath)
        {
                var className = Path.GetFileNameWithoutExtension(filePath);
                var classType = Type.GetType(className);
                if (classType == null) return null;
                var fieldInfo = classType.GetField("tutorialType", BindingFlags.Static |BindingFlags.Instance | BindingFlags.NonPublic);
                var tutorialType = fieldInfo?.GetValue(classType);
                return (TutorialType?)tutorialType;
        }
}
