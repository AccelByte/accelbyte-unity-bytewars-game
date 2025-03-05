// Copyright (c) 2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

public enum GameEntityType
{
    Player,
    Missile,
    Planet
}

public enum GameEntityDestroyReason
{
    None = 0,
    Lifetime,
    PlayerHit,
    PlanetHit,
    MissileHit,
    PowerUp
};
