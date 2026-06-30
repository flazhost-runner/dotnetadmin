using System.Text.Json;

namespace DotNetAdmin.Core.Helpers;

public static class ValidationSessionHelper
{
    public static void SetFieldErrors(this ISession session,
        Dictionary<string, string> errors,
        Dictionary<string, string> old)
    {
        session.SetString("field_errors", JsonSerializer.Serialize(errors));
        session.SetString("old_input", JsonSerializer.Serialize(old));
    }

    public static (Dictionary<string, string> errors, Dictionary<string, string> old) GetFieldErrors(
        this ISession session)
    {
        var errorsJson = session.GetString("field_errors");
        var oldJson = session.GetString("old_input");
        session.Remove("field_errors");
        session.Remove("old_input");
        return (
            errorsJson != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(errorsJson)! : [],
            oldJson != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(oldJson)! : []
        );
    }
}
