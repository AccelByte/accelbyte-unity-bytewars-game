using System.Collections.Generic;
using AccelByte.Models;

public class CreateMatchConfig
{
    private const string CreatedMatchAttributeKey = "create_match_session";
    private const int CreatedMatchAttributeValue = 1;
    public static readonly Dictionary<string, object> CreatedMatchSessionAttribute = new Dictionary<string, object>()
    {
        {CreatedMatchAttributeKey, CreatedMatchAttributeValue}
    };
}
