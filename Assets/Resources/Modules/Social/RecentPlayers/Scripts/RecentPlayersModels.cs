// Copyright (c) 2025 - 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using AccelByte.Models;

[Serializable]
public class RecentPlayerInfo
{
    public string UserId;
	public string DisplayName;
	public string AvatarUrl;
	public UserStatus Availability;
	public SessionV2MemberStatus SessionStatus;
	public string Activity;
	public DateTime LastSeenAt;
}
