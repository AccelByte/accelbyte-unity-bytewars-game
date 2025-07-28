// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Models;
using UnityEngine;

public class AccelByteWarsOnlineSessionModels
{
    public static readonly string ServerNameAttributeKey = "server_name";
    public static readonly string ClientVersionAttributeKey = "client_version";

    public static readonly string StartingAsHostMessage = "Starting As Host";
    public static readonly string WaitingHostMessage = "Waiting for Host";

    public const string NoneSessionTemplateName = "unity-elimination-none";
    public const string EliminationDSSessionTemplateName = "unity-elimination-ds";
    public const string EliminationP2PSessionTemplateName = "unity-elimination-p2p";
    public const string TeamDeathmatchDSSessionTemplateName = "unity-teamdeathmatch-ds";
    public const string TeamDeathmatchP2PSessionTemplateName = "unity-teamdeathmatch-p2p";
    public const string EliminationDSAMSSessionTemplateName = "unity-elimination-ds-ams";
    public const string TeamDeathmatchDSAMSSessionTemplateName = "unity-teamdeathmatch-ds-ams";

    public static readonly string ClientVersion = TutorialModuleUtil.IsOverrideDedicatedServerVersion() ? Application.version : string.Empty;

    public static readonly Dictionary<InGameMode, Dictionary<GameSessionServerType, SessionV2GameSessionCreateRequest>> SessionCreateRequestModels = new()
    {
        { 
            InGameMode.None, new()
            {
                {
                    GameSessionServerType.None, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.NONE,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = NoneSessionTemplateName,
                        matchPool = NoneSessionTemplateName,
                    }
                } 
            }
        },
        { 
            InGameMode.MatchmakingElimination, new()
            {
                {
                    GameSessionServerType.DedicatedServer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationDSSessionTemplateName,
                        matchPool = EliminationDSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                },
                {
                    GameSessionServerType.PeerToPeer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.P2P,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationP2PSessionTemplateName,
                        matchPool = EliminationP2PSessionTemplateName,
                    }
                },
                {
                    GameSessionServerType.DedicatedServerAMS, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationDSAMSSessionTemplateName,
                        matchPool = EliminationDSAMSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                }
            }
        },
        { 
            InGameMode.MatchmakingTeamDeathmatch, new()
            {
                {
                    GameSessionServerType.DedicatedServer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchDSSessionTemplateName,
                        matchPool = TeamDeathmatchDSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                },
                {
                    GameSessionServerType.PeerToPeer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.P2P,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchP2PSessionTemplateName,
                        matchPool = TeamDeathmatchP2PSessionTemplateName,
                    }
                },
                {
                    GameSessionServerType.DedicatedServerAMS, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchDSAMSSessionTemplateName,
                        matchPool = TeamDeathmatchDSAMSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                }
            }
        },
        { 
            InGameMode.CreateMatchElimination, new() 
            {
                {
                    GameSessionServerType.DedicatedServer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationDSSessionTemplateName,
                        matchPool = EliminationDSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                },
                {
                    GameSessionServerType.PeerToPeer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.P2P,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationP2PSessionTemplateName,
                        matchPool = EliminationP2PSessionTemplateName,
                    }
                },
                {
                    GameSessionServerType.DedicatedServerAMS, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = EliminationDSAMSSessionTemplateName,
                        matchPool = EliminationDSAMSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                }
            }
        },
        { 
            InGameMode.CreateMatchTeamDeathmatch, new() 
            {
                {
                    GameSessionServerType.DedicatedServer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchDSSessionTemplateName,
                        matchPool = TeamDeathmatchDSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                },
                {
                    GameSessionServerType.PeerToPeer, 
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.P2P,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchP2PSessionTemplateName,
                        matchPool = TeamDeathmatchP2PSessionTemplateName,
                    }
                },
                {
                    GameSessionServerType.DedicatedServerAMS,
                    new SessionV2GameSessionCreateRequest()
                    {
                        type = SessionConfigurationTemplateType.DS,
                        joinability = SessionV2Joinability.OPEN,
                        configurationName = TeamDeathmatchDSAMSSessionTemplateName,
                        matchPool = TeamDeathmatchDSAMSSessionTemplateName,
                        clientVersion = ClientVersion
                    }
                }
            }
        }
    };

    public static SessionV2GameSessionCreateRequest GetGameSessionRequestModel(
        InGameMode gameMode, 
        GameSessionServerType serverType)
    {
        if (!SessionCreateRequestModels.TryGetValue(gameMode, out var matchTypeDict))
        {
            return null;
        }

        matchTypeDict.TryGetValue(serverType, out var request);
        return request;
    }

    public static InGameMode GetGameSessionGameMode(SessionV2GameSession session)
    {
        if (session == null)
        {
            return InGameMode.None;
        }

        bool isMatchmaking = !session.attributes.ContainsKey(MatchSessionEssentialsModels.MatchSessionAttributeKey);
        switch (session.configuration.name)
        {
            case EliminationDSSessionTemplateName:
            case EliminationDSAMSSessionTemplateName:
            case EliminationP2PSessionTemplateName:
                return isMatchmaking ? InGameMode.MatchmakingElimination : InGameMode.CreateMatchElimination;
            case TeamDeathmatchDSSessionTemplateName:
            case TeamDeathmatchDSAMSSessionTemplateName:
            case TeamDeathmatchP2PSessionTemplateName:
                return isMatchmaking ? InGameMode.MatchmakingTeamDeathmatch : InGameMode.CreateMatchTeamDeathmatch;
            default:
                return InGameMode.None;
        }
    }

    public static GameSessionServerType GetGameSessionServerType(SessionV2GameSession session)
    {
        if (session == null)
        {
            return GameSessionServerType.None;
        }

        switch (session.configuration.name)
        {
            case EliminationP2PSessionTemplateName:
            case TeamDeathmatchP2PSessionTemplateName:
                return GameSessionServerType.PeerToPeer;
            case EliminationDSSessionTemplateName:
            case TeamDeathmatchDSSessionTemplateName:
                return GameSessionServerType.DedicatedServer;
            case EliminationDSAMSSessionTemplateName:
            case TeamDeathmatchDSAMSSessionTemplateName:
                return GameSessionServerType.DedicatedServerAMS;
            default:
                return GameSessionServerType.None;
        }
    }
}
