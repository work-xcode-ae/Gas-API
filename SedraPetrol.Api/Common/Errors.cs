namespace SedraPetrol.Api.Common;

/// <summary>An error that is safe to show to the user. Mapped to a ProblemDetails response.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public string? Code { get; }
    public AppException(string message, int statusCode = StatusCodes.Status400BadRequest, string? code = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.", StatusCodes.Status404NotFound, "not_found") { }
}

/// <summary>A business rule was violated (e.g. a second active contract on the same unit).</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? code = null)
        : base(message, StatusCodes.Status409Conflict, code ?? "business_rule") { }
}
