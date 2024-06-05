// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class FriendsHelper
{
    public const string RequestSentMessage = "Request Sent";
    public const string ErrorStatusMessage = "Error";

    public static ReadOnlyDictionary<RelationshipStatusCode, string> StatusMessageMap = new(
        new Dictionary<RelationshipStatusCode, string>()
        {
            { RelationshipStatusCode.Friend, "Already Friends" },
            { RelationshipStatusCode.Outgoing, "Request Pending" },
            { RelationshipStatusCode.Incoming, "Awaiting Response" },
            { RelationshipStatusCode.NotFriend, "Not Friends" },
        });

    private static bool IsModuleActive()
    {
        return TutorialModuleManager.Instance.IsModuleActive(TutorialType.FriendsEssentials);
    }
    
    private static bool IsStarterModeActive()
    {
        ModuleModel module = TutorialModuleManager.Instance.GetModule(TutorialType.FriendsEssentials);

        return module.isStarterActive;
    }

    public static AssetEnum GetMenuByDependencyModule()
    {
        return IsModuleActive() && IsStarterModeActive() 
            ? AssetEnum.FriendDetailsMenuCanvas_Starter : AssetEnum.FriendDetailsMenuCanvas;
    }
}