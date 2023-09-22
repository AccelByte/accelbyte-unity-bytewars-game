using System.Collections.Generic;
using AccelByte.Models;

public class MatchSessionConfig
{
    private const string CreatedMatchAttributeKey = "cm";
    private const int CreatedMatchAttributeValue = 1;
    public static readonly Dictionary<string, object> CreatedMatchSessionAttribute = new Dictionary<string, object>()
    {
        {CreatedMatchAttributeKey, CreatedMatchAttributeValue}
    };
    public const string UnitySessionEliminationDs = "unity-elimination-ds";
    public const string UnitySessionEliminationP2P = "unity-elimination-p2p";
    public const string UnitySessionDeathMatchDs = "unity-teamdeathmatch-ds";
    public const string UnitySessionDeathMatchP2P = "unity-teamdeathmatch-p2p";
    public static readonly Dictionary<InGameMode, Dictionary<MatchSessionServerType, SessionV2GameSessionCreateRequest>>
            MatchRequests =
                new Dictionary<InGameMode, Dictionary<MatchSessionServerType, SessionV2GameSessionCreateRequest>>()
                {
                    { InGameMode.CreateMatchEliminationGameMode, new Dictionary<MatchSessionServerType, 
                        SessionV2GameSessionCreateRequest>() {
                    {
                        MatchSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                        {
                            type = SessionConfigurationTemplateType.DS,
                            joinability = SessionV2Joinability.OPEN,
                            configurationName = UnitySessionEliminationDs,
                            attributes = CreatedMatchSessionAttribute
                        }
                    },
                    {
                        MatchSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                        {
                            type = SessionConfigurationTemplateType.P2P,
                            joinability = SessionV2Joinability.OPEN,
                            configurationName = UnitySessionEliminationP2P,
                            attributes = CreatedMatchSessionAttribute
                        }
                    }
                    }},
                    { InGameMode.CreateMatchDeathMatchGameMode, new Dictionary<MatchSessionServerType, SessionV2GameSessionCreateRequest>(){
                    {
                        MatchSessionServerType.DedicatedServer, new SessionV2GameSessionCreateRequest()
                        {
                            type = SessionConfigurationTemplateType.DS,
                            joinability = SessionV2Joinability.OPEN,
                            configurationName = UnitySessionDeathMatchDs,
                            attributes = CreatedMatchSessionAttribute
                        }
                    },
                    {
                        MatchSessionServerType.PeerToPeer, new SessionV2GameSessionCreateRequest()
                        {
                            type = SessionConfigurationTemplateType.P2P,
                            joinability = SessionV2Joinability.OPEN,
                            configurationName = UnitySessionDeathMatchP2P,
                            attributes = CreatedMatchSessionAttribute
                        }
                    }
                    }}
                };
}
