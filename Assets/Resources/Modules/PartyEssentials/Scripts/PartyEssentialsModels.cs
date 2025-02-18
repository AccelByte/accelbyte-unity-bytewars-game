// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using System;
using UnityEngine;

public class PartyEssentialsModels 
{
    public static readonly string PartySessionTemplateName = "unity-party";
    public static readonly int PartyMaxMembers = 4;

    public static readonly string PartyPopUpMessage = "Party";
    public static readonly string JoinNewPartyConfirmationMessage = "Leave Current Party and Join a New One?";

    public static readonly string SuccessSendPartyInviteMessage = "Party Invitation Sent";
    public static readonly string FailedSendPartyInviteMessage = "Failed to Send Party Invitation";

    public static readonly string PartyNewLeaderMessage = "Is Now the Party Leader";
    public static readonly string PartyMemberJoinedMessage = "Joined the Party";
    public static readonly string PartyMemberLeftMessage = "Left the Party";
    public static readonly string PartyInviteReceivedMessage = "Invites You to Party";
    public static readonly string PartyInviteRejectedMessage = "Rejected Your Party Invite";
    public static readonly string KickedFromPartyMessage = "Kicked From the Party";

    public static readonly string AcceptPartyInviteMessage = "Accept";
    public static readonly string RejectPartyInviteMessage = "Reject";

    public struct PartyDetailsModel
    {
        public SessionV2PartySession PartySession;
        public BaseUserInfo[] MemberUserInfos;
    }

    /// <summary>
    /// Party Helper is a class containing boiler plate code so other modules 
    /// can use party essentials without accessing the wrapper directly.
    /// This promote modularity between tutorial modules.
    /// This class should be not exposed to user nor public documentation.
    /// </summary>
    public static class PartyHelper
    {
        private static PartyEssentialsWrapper partyWrapper;
        private static PartyEssentialsWrapper_Starter partyWrapperStarter;
        private static Action cachedOnPartyUpdateDelegate = delegate { };

        public static void Initialize(MonoBehaviour wrapper) 
        {
            ModuleModel partyModule = TutorialModuleManager.Instance.GetModule(TutorialType.PartyEssentials);
            if (partyModule == null || !partyModule.isActive) 
            {
                BytewarsLogger.LogWarning("Cannot initialize party helper. The party essentials module is not active.");
                return;
            }

            // Initialize default wrapper.
            if (!partyModule.isStarterActive && wrapper is PartyEssentialsWrapper)
            {
                partyWrapper = (PartyEssentialsWrapper)wrapper;

                // Assign cached delegates with no duplicates.
                foreach (Delegate invocation in cachedOnPartyUpdateDelegate.GetInvocationList())
                {
                    partyWrapper.OnPartyUpdateDelegate -= (Action)invocation;
                    partyWrapper.OnPartyUpdateDelegate += (Action)invocation;
                }
            }
            // Initialize starter wrapper.
            else if (partyModule.isStarterActive && wrapper is PartyEssentialsWrapper_Starter)
            {
                partyWrapperStarter = (PartyEssentialsWrapper_Starter)wrapper;

                // Assign cached delegates with no duplicates.
                foreach (Delegate invocation in cachedOnPartyUpdateDelegate.GetInvocationList())
                {
                    partyWrapperStarter.OnPartyUpdateDelegate -= (Action)invocation;
                    partyWrapperStarter.OnPartyUpdateDelegate += (Action)invocation;
                }
            }
            else 
            {
                BytewarsLogger.LogWarning("Cannot initialize party helper. Invalid party wrapper type.");
            }
        }

        public static void Deinitialize() 
        {
            if (partyWrapper)
            {
                foreach (Delegate invocation in cachedOnPartyUpdateDelegate.GetInvocationList())
                {
                    partyWrapper.OnPartyUpdateDelegate -= (Action)invocation;
                }
            }
            else if (partyWrapperStarter)
            {
                foreach (Delegate invocation in cachedOnPartyUpdateDelegate.GetInvocationList())
                {
                    partyWrapperStarter.OnPartyUpdateDelegate -= (Action)invocation;
                }
            }

            cachedOnPartyUpdateDelegate = delegate { };
            partyWrapper = null;
            partyWrapperStarter = null;
        }

        public static SessionV2PartySession CurrentPartySession 
        {
            get 
            {
                if (partyWrapper)
                {
                    return partyWrapper.CurrentPartySession;
                }
                else if (partyWrapperStarter)
                {
                    return partyWrapperStarter.CurrentPartySession;
                }
                else
                {
                    BytewarsLogger.LogWarning("Failed to send party invitation. Party essentials module is not active.");
                    return null;
                }
            }
        }

        public static void OnInviteToPartyButtonClicked(string userId) 
        {
            if (partyWrapper) 
            {
                partyWrapper.OnInviteToPartyButtonClicked(userId);
            }
            else if (partyWrapperStarter) 
            {
                partyWrapperStarter.OnInviteToPartyButtonClicked(userId);
            }
            else 
            {
                BytewarsLogger.LogWarning("Failed to send party invitation. Party essentials module is not active.");
            }
        }

        public static void OnKickPlayerFromPartyButtonClicked(string userId)
        {
            if (partyWrapper)
            {
                partyWrapper.OnKickPlayerFromPartyButtonClicked(userId);
            }
            else if (partyWrapperStarter)
            {
                partyWrapperStarter.OnKickPlayerFromPartyButtonClicked(userId);
            }
            else
            {
                BytewarsLogger.LogWarning("Failed to kick party member. Party essentials module is not active.");
            }
        }

        public static void OnPromotePartyLeaderButtonClicked(string userId) 
        {
            if (partyWrapper)
            {
                partyWrapper.OnPromotePartyLeaderButtonClicked(userId);
            }
            else if (partyWrapperStarter)
            {
                partyWrapperStarter.OnPromotePartyLeaderButtonClicked(userId);
            }
            else
            {
                BytewarsLogger.LogWarning("Failed to promote party leader. Party essentials module is not active.");
            }
        }

        public static void BindOnPartyUpdate(Action eventToBind)
        {
            if (partyWrapper)
            {
                partyWrapper.OnPartyUpdateDelegate += eventToBind;
            }
            else if (partyWrapperStarter)
            {
                partyWrapperStarter.OnPartyUpdateDelegate += eventToBind;
            }
            else
            {
                // If no wrapper is initialized, store to cache first.
                cachedOnPartyUpdateDelegate += eventToBind;
            }
        }

        public static void UnBindOnPartyUpdate(Action eventToUnbind) 
        {
            cachedOnPartyUpdateDelegate -= eventToUnbind;

            if (partyWrapper)
            {
                partyWrapper.OnPartyUpdateDelegate -= eventToUnbind;
            }
            else if (partyWrapperStarter)
            {
                partyWrapperStarter.OnPartyUpdateDelegate -= eventToUnbind;
            }
        }
    }
}
