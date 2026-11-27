using Happymeal.Common.Validation;

namespace Happymeal.Common.Results;

public sealed record Error(
    ErrorType Type,
    string Message,
    string Code = ErrorCodes.Failure)
{
    private static readonly IReadOnlyDictionary<string, string[]> _noValidationErrors = new Dictionary<string, string[]>();

    /// <summary>Property path to messages. Only filled when <see cref="Type"/> is <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; private init; } = _noValidationErrors;
    public IReadOnlyList<ValidationFailure> ValidationFailures { get; private init; } = [];

    public static Error Failure(string message, string code = ErrorCodes.Failure) => new(ErrorType.Failure, message, code);

    public static Error BadRequest(string message, string code = ErrorCodes.InvalidRequest) => new(ErrorType.BadRequest, message, code);

    public static Error NotFound(string message, string code = ErrorCodes.NotFound) => new(ErrorType.NotFound, message, code);

    public static Error Conflict(string message, string code = ErrorCodes.Conflict) => new(ErrorType.Conflict, message, code);

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors)
    {
        return Validation([.. validationErrors.SelectMany(field => field.Value.Select(
            message => new ValidationFailure(field.Key, message)))]);
    }

    public static Error Validation(IReadOnlyList<ValidationFailure> failures)
    {
        return new(ErrorType.Validation, "One or more validation errors occurred.", ErrorCodes.Validation)
        {
            ValidationFailures = [.. failures],
            ValidationErrors = failures.GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.Message).Distinct().ToArray())
        };
    }
}
