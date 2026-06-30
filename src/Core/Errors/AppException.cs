namespace DotNetAdmin.Core.Errors;

public class AppException : Exception
{
    public int StatusCode { get; }
    public Dictionary<string, string>? Errors { get; }

    public AppException(string message, int statusCode = 400, Dictionary<string, string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundAppException : AppException
{
    public NotFoundAppException(string message = "Resource not found")
        : base(message, 404) { }
}

public class ConflictAppException : AppException
{
    public ConflictAppException(string message = "Resource already exists")
        : base(message, 409) { }
}

public class ValidationAppException : AppException
{
    public ValidationAppException(string message = "Validation failed", Dictionary<string, string>? errors = null)
        : base(message, 422, errors) { }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Unauthorized")
        : base(message, 401) { }
}

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "Forbidden")
        : base(message, 403) { }
}
