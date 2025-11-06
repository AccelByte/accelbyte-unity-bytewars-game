// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using System.Linq;
using System.Collections.Generic;

public class PartyEssentialsWrapper_Starter : SessionEssentialsWrapper
{
    #region Party Action Button Helper
    public void OnInviteToPartyButtonClicked(string inviteeUserId)
    {
        // TODO: Call function to send party invite.
    }

    public void OnKickPlayerFromPartyButtonClicked(string targetUserId)
    {
        // TODO: Call function to kick player from party.
    }

    public void OnPromotePartyLeaderButtonClicked(string targetUserId)
    {
        // TODO: Call function to promote party leader.
    }
    #endregion

    private void OnEnable()
    {
        PartyEssentialsModels.PartyHelper.Initialize(this);

        // TODO: Bind your party delegates.
    }

    private void OnDisable()
    {
        PartyEssentialsModels.PartyHelper.Deinitialize();

        // TODO: Unbind your party delegates.
    }
}
