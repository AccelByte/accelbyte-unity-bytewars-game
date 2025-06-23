// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class CloudSaveEssentialsWrapper_Starter : MonoBehaviour
{
    // AccelByte's Multi Registry references
    private CloudSave cloudSave;
    private Lobby lobby;

    void Start()
    {
        cloudSave = AccelByteSDK.GetClientRegistry().GetApi().GetCloudSave();
        lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();

        // TODO: Add the tutorial module code here.
    }

    #region Helper Functions

    public void LoadGameOptions()
    {
        // TODO: Add code to load game options from Cloud Save here.
    }

    private void SaveGameOptions(float musicVolume, float sfxVolume)
    {
        // TODO: Add code to save game options to Cloud Save.
    }

    #endregion

    // TODO: Declare the tutorial module functions here.
}
