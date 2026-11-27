# Validation Strategy

Happymeal-BE uses the standard ASP.NET Core error contract. Application errors are
RFC 9457 `ProblemDetails` bodies with a stable `code`. Validation errors are a
`400` `ValidationProblemDetails` with existing plain text messages plus structured
per-field codes and interpolation parameters. See the
[public error-code catalog](../../../docs/api-error-codes.md).

Domain factories and mutators validate inputs and return failures as an `Error` of type `Validation`. The API maps that error to the HTTP response.

## Request Flow

```text
HTTP request (DTO)
  -> Model binding
     -> invalid JSON or shape: 400 ValidationProblemDetails ([ApiController] automatic response)
  -> Controller -> command/query -> ISender -> handler
  -> Domain factory/mutator calls params.Validate()
     -> failures: ToValidationError() -> Error.Validation -> 400 ValidationProblemDetails
  -> business outcome: Error.NotFound / Conflict / BadRequest -> 404 / 409 / 400 ProblemDetails
  -> success: 200 / 201 / 204
```

## Error Contract

`ControllerExtensions.ToActionResult` maps results:

| Result | HTTP | Body |
|--------|------|------|
| `Result` success | 204 | none |
| `Result<T>` success | 200, or the `onSuccess` override (201 on create) | value |
| `ErrorType.Validation` | 400 | `ValidationProblemDetails` (`errors`: camelCase path -> messages) |
| `ErrorType.BadRequest` | 400 | `ProblemDetails` with `detail` |
| `ErrorType.NotFound` | 404 | `ProblemDetails` with `detail` |
| `ErrorType.Conflict` | 409 | `ProblemDetails` with `detail` |
| `ErrorType.Failure` | 500 | `ProblemDetails` with `detail` |

All `ProblemDetails` bodies get `type`, `title`, `status` and `traceId` from the ASP.NET Core `ProblemDetailsFactory`. Unhandled exceptions become a `500` ProblemDetails through `UseExceptionHandler` + `AddProblemDetails`.

Example:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "code": "validation.failed",
  "errors": { "name": [ "Name is required." ] },
  "validationErrors": { "name": [{ "code": "validation.required", "parameters": {} }] },
  "traceId": "00-..."
}
```

## Layer Placement

| Layer | Responsibility |
|-------|----------------|
| Domain (`Params/*` implementing `IValidatable`) | Business invariants on the input. `Create`/`Update` call `Validate()` before changing state. Always the source of truth, so every entry point gets the same rules. |
| Application | Checks that need data or collaborators (existence -> `Error.NotFound`, uniqueness -> `Error.Conflict`). For a rule about the request shape only (for example paging limits), write it by hand at the start of `HandleAsync`, or make the command `IValidatable`, and return `ToValidationError()`. |
| API | Model binding only. Maps `Result` to status codes and C# paths to JSON paths. No business validation in controllers. |

Do not duplicate a Domain rule in Application. There is no FluentValidation in the solution. Ask before adding it, and never in Domain.

## Messages and Paths

- Keep English diagnostic messages for compatibility. Clients translate stable
  `Error.Code` and `ValidationFailure.Code`, not those messages. Use named constants
  from `RecipeErrorCodes`, `ErrorCodes` and `ValidationErrorCodes`; codes never
  depend on the selected language or on message parsing.
- `ValidationFailure.Parameters` carries interpolation values such as `max` and
  `min`. Preserve codes and parameters when remapping a validation failure to a
  nested property path. Every new rule needs an appropriate code.
- `ValidationFailure.PropertyName` is the C# path (`Name`, `Steps[0].Text`).
  `ProblemDetailsMapper` converts each segment with the configured JSON naming
  policy (`name`, `steps[0].text`) for both `errors` and `validationErrors`.
- Model-binding failures get `request.invalid` and per-field `request.invalidValue`
  without interpreting framework messages. Unexpected exceptions get
  `error.unexpected` without disclosing internal details.

## Testing

Use the `unit-tests` skill for validator and handler tests. Assert on property paths and error types, not on full message text. Use the `integration-tests` skill only to prove the HTTP shape end-to-end.
