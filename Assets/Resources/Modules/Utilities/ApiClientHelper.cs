using AccelByte.Api;
using AccelByte.Core;

public static class ApiClientHelper
{
    public static bool IsPlayerLoggedIn 
    { 
        get 
        { 
            return user?.Session?.IsValid() ?? false;
        }
    }
     
    private static ApiClient apiClient = AccelByteSDK.GetClientRegistry()?.GetApi();

    private static User user = apiClient?.GetUser();
}
