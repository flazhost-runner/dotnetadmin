namespace DotNetAdmin.Core.Helpers;

public static class ResponseHelper
{
    public static IResult ApiSuccess(string message, object? data = null) =>
        Results.Ok(new { status = true, message, data });

    public static IResult ApiError(string message, int statusCode = 400, object? errors = null) =>
        Results.Json(new { status = false, message, errors }, statusCode: statusCode);

    public static JsonResult Success(this ControllerBase _, string message, object? data = null) =>
        new(new { status = true, message, data });

    public static JsonResult Error(this ControllerBase _, string message, int statusCode = 400, object? errors = null) =>
        new(new { status = false, message, errors }) { StatusCode = statusCode };
}
