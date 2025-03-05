// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using UnityEngine;

public abstract class GameEntityAbs : MonoBehaviour
{
    public abstract float GetScale();
    public abstract float GetRadius();
    public abstract float GetMass();
    public abstract void OnHitByMissile();
    public abstract void Reset();
    public abstract void SetId(int id);
    public abstract int GetId();
}
