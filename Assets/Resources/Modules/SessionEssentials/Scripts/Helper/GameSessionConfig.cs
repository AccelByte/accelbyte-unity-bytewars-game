// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System.Collections.Generic;
using AccelByte.Models;
using UnityEngine;

public class GameSessionConfig
{
    public const string UnitySessionNone = "unity-elimination-none";
    public const string UnitySessionEliminationDS = "unity-elimination-ds";
    public const string UnitySessionEliminationP2P = "unity-elimination-p2p";
    public const string UnitySessionTeamDeathmatchDS = "unity-teamdeathmatch-ds";
    public const string UnitySessionTeamDeathmatchP2P = "unity-teamdeathmatch-p2p";
    public const string UnitySessionEliminationDSAMS = "unity-elimination-ds-ams";
    public const string UnitySessionTeamDeathmatchDSAMS = "unity-teamdeathmatch-ds-ams";

    private static readonly string UnitySessionClientVersion = TutorialModuleUtil.IsOverrideDedicatedServerVersion() ? Application.version : string.Empty;

    public static readonly Dictionary<InGameMode, Dictionary<GameSessionServerType, SessionV2GameSessionCreateRequest>>
            SessionCreateRequest =
                new Dictionary<InGameMode, Dictionary<GameSessionServerType, SessionV2GameSessionCreateRequest>>()
                {
                    { InGameMode.None, new Dictionary<GameSessionServerType,
                        SessionV2GameSessionCreateRequest>()
                        {
                            {
                                GameSessionServerType.None, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.NONE,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionNone,
                                    matchPool = UnitySessionNone,
                                }
                            } 
                        }
                    },
                    { InGameMode.OnlineEliminationGameMode, new Dictionary<GameSessionServerType,
                        SessionV2GameSessionCreateRequest>()
                        {
                            {
                                GameSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationDS,
                                    matchPool = UnitySessionEliminationDS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            },
                            {
                                GameSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.P2P,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationP2P,
                                    matchPool = UnitySessionEliminationP2P,
                                }
                            },
                            {
                                GameSessionServerType.DedicatedServerAMS, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationDSAMS,
                                    matchPool = UnitySessionEliminationDSAMS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            }
                        }
                    },
                    { InGameMode.OnlineDeathMatchGameMode, new Dictionary<GameSessionServerType,
                        SessionV2GameSessionCreateRequest>()
                        {
                            {
                                GameSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchDS,
                                    matchPool = UnitySessionTeamDeathmatchDS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            },
                            {
                                GameSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.P2P,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchP2P,
                                    matchPool = UnitySessionTeamDeathmatchP2P,
                                }
                            },
                            {
                                GameSessionServerType.DedicatedServerAMS, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchDSAMS,
                                    matchPool = UnitySessionTeamDeathmatchDSAMS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            }
                        }
                    },
                    { InGameMode.CreateMatchEliminationGameMode, new Dictionary<GameSessionServerType, 
                        SessionV2GameSessionCreateRequest>() 
                        {
                            {
                                GameSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationDS,
                                    matchPool = UnitySessionEliminationDS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            },
                            {
                                GameSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.P2P,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationP2P,
                                    matchPool = UnitySessionEliminationP2P,
                                }
                            },
                            {
                                GameSessionServerType.DedicatedServerAMS, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionEliminationDSAMS,
                                    matchPool = UnitySessionEliminationDSAMS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            }
                        }
                    },
                    { InGameMode.CreateMatchDeathMatchGameMode, new Dictionary<GameSessionServerType, SessionV2GameSessionCreateRequest>(){
                            {
                                GameSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchDS,
                                    matchPool = UnitySessionTeamDeathmatchDS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            },
                            {
                                GameSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.P2P,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchP2P,
                                    matchPool = UnitySessionTeamDeathmatchP2P,
                                }
                            },
                            {
                                GameSessionServerType.DedicatedServerAMS, new SessionV2GameSessionCreateRequest()
                                {
                                    type = SessionConfigurationTemplateType.DS,
                                    joinability = SessionV2Joinability.OPEN,
                                    configurationName = UnitySessionTeamDeathmatchDSAMS,
                                    matchPool = UnitySessionTeamDeathmatchDSAMS,
                                    clientVersion = UnitySessionClientVersion
                                }
                            }
                        }
                    }
                };
}
