namespace DotNetAdmin.Core.Helpers;

public static class FlashHelper
{
    public static void SetFlash(this ISession session, string key, string message)
    {
        session.SetString("flash_key", key);
        session.SetString("flash_message", message);
    }

    public static void SetSuccess(this ISession session, string message) =>
        session.SetFlash("success", message);

    public static void SetError(this ISession session, string message) =>
        session.SetFlash("error", message);
}
