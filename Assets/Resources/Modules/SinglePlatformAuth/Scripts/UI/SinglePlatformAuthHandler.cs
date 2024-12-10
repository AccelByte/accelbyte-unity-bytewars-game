using UnityEngine;

public class SinglePlatformAuthHandler : MenuCanvas
{
    public override GameObject GetFirstButton()
    {
        return null;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.SinglePlatformAuth;
    }
}
