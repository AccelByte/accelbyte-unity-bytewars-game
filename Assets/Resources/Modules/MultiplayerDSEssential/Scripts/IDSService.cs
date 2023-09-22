// // Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// // This is licensed software from AccelByte Inc, for limitations
// // and restrictions contact your company contract manager.

using System;
using AccelByte.Core;

public interface IDSService
{
        event Action OnInstantiateComplete;
        event ResultCallback OnLoginCompleteEvent;
        event ResultCallback OnRegisterCompleteEvent;
        event ResultCallback OnUnregisterCompleteEvent;
        
        void LoginServer();
        void RegisterServer();
        void ConnectToDSHub();
        void ListenOnDisconnect();
        void UnregisterServer();
        
}