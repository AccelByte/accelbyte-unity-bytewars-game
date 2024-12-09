// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Core;
using AccelByte.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class FriendsHelper
{
    public const string PromptConfirmTitle = "Confirmation";
    public const string PromptMessageTitle = "Message";
    public const string PromptErrorTitle = "Error";

    public const string FriendCodeCopiedMessage = "Copied!";
    public const string FriendCodePreloadMessage = "...";

    public const string QueryNotFoundMessage = "No results found for query '%QUERY%'.";

    public const string SendingFriendRequestMessage = "Sending Friend Request";
    public const string AcceptingFriendRequestMessage = "Accepting Friend Request";
    public const string RejectingFriendRequestMessage = "Rejecting Friend Request";
    public const string CancelingFriendRequestMessage = "Canceling Friend Request";

    public const string UnfriendingMessage = "Unfriending";
    public const string BlockingPlayerMessage = "Blocking Player";
    public const string UnblockingPlayerMessage = "Unblocking Player";

    public const string FriendRequestSelfMessage = "Cannot send friend request to yourself";
    public const string FriendRequestSentDetailsMessage = "Friend Request has been sent successfully";
    public const string FriendRequestSentFriendCodeMessage = "Friend Request sent via Friend Code";

    public const string FriendRequestAcceptedMessage = "Friend Request Accepted";
    public const string FriendRequestRejectedMessage = "Friend Request Rejected";
    public const string FriendRequestCanceledMessage = "Friend Request Canceled";

    public const string UnfriendConfirmationMessage = "Are you sure you want to unfriend this player?";
    public const string BlockPlayerConfirmationMessage = "Are you sure you want to block this player?";
    public const string UnblockPlayerConfirmationMessage = "Are you sure you want to unblock this player?";

    public const string UnfriendCompletedMessage = "You are no longer friends with this player";
    public const string BlockPlayerCompletedMessage = "Player has been blocked";
    public const string UnblockPlayerCompletedMessage = "Player has been unblocked";

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

    // TODO: Update this error code when SDK has the correct error code for this case.
    private const ErrorCode FriendRequestAlreadySent = (ErrorCode)11973;
    private const ErrorCode FriendRequestAwaitingResponse = (ErrorCode)11974;

    public const string DefaultSendFriendRequestErrorMessage = "Failed to send friend request, Please try again later";
    public static Dictionary<ErrorCode, string> SendFriendRequestErrorMessages = new()
    {
        { FriendRequestAlreadySent, "You have already sent a friend request to this user" },
        { FriendRequestAwaitingResponse, "This user has already sent you a friend request" },
        { ErrorCode.FriendRequestConflictFriendship, "You are already friends with this user" },
        { ErrorCode.PlayerBlocked, "This user has blocked you or you have blocked this user" }
    };

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