using Happymeal.Common.Results;

namespace Happymeal.Common.Validation;

public static class ValidationFailureExtensions
{
    extension(IEnumerable<ValidationFailure> failures)
    {
        /// <summary>Groups the failures by property into an <see cref="ErrorType.Validation"/> error, or returns null when there are none.</summary>
        public Error? ToValidationError()
        {
            ValidationFailure[] errors = [.. failures];
            return errors.Length == 0 ? null : Error.Validation(errors);
        }
    }
}
