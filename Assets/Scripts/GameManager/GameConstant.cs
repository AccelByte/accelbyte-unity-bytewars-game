// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public class GameConstant 
{
    public const string MenuSceneName = "MainMenuScene";
    public const int MenuSceneBuildIndex = 0;
    public const string GameSceneName = "GalaxyWorld";
    public const int GameSceneBuildIndex = 1;

    public const int MaxConnectAttemptsSec = 60;

    public const float DefaultOrthographicSize = 15f;
    public static Vector2 MaxMissileArea = new(70, 40);
}
