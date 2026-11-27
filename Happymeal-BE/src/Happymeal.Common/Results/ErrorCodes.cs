namespace Happymeal.Common.Results;

public static class ErrorCodes
{
    public const string Failure = "error.failure";
    public const string Unexpected = "error.unexpected";
    public const string InvalidRequest = "request.invalid";
    public const string InvalidRequestValue = "request.invalidValue";
    public const string NotFound = "resource.notFound";
    public const string Conflict = "resource.conflict";
    public const string Validation = "validation.failed";
}
