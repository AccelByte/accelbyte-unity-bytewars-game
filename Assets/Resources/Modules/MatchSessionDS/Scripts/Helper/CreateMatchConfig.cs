using System.Collections.Generic;
using AccelByte.Models;

public class CreateMatchConfig
{
    private const string CreatedMatchAttributeKey = "cm";
    private const int CreatedMatchAttributeValue = 1;
    public static readonly Dictionary<string, object> CreatedMatchSessionAttribute = new Dictionary<string, object>()
    {
        {CreatedMatchAttributeKey, CreatedMatchAttributeValue}
    };
}
