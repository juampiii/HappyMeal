# API error codes

Clients localize error messages using stable codes, field paths and parameters.
Do not compare or parse English `detail`, `title` or `errors` messages to select a
translation. These remain available as human-readable diagnostics and for clients
using the previous response shape. HTTP status codes and success bodies are unchanged.

Every application ProblemDetails response includes a top-level `code`. Validation
responses also include `validationErrors`: JSON field path to an array of objects
with `code` and `parameters`. Field paths match the existing `errors` dictionary;
indexed paths such as `ingredients[0].unit` remain indexed. Parameters without
values serialize as `{}`. Parameters are data for interpolation, not translated text.

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "code": "validation.failed",
  "errors": {
    "tags[0]": ["Tag name must be 50 characters or fewer."]
  },
  "validationErrors": {
    "tags[0]": [
      { "code": "validation.maxLength", "parameters": { "max": 50 } }
    ]
  },
  "traceId": "..."
}
```

## Recipe outcomes

| Code | HTTP | Meaning |
| --- | --- | --- |
| `recipe.notFound` | 404 | Requested recipe does not exist. |
| `recipe.nameAlreadyExists` | 409 | Another visible recipe reserves the name. |
| `recipe.idAlreadyExists` | 409 | The requested recipe ID already exists. |
| `recipe.notEditable` | 409 | Published or hidden content cannot be edited. |
| `recipe.hidden` | 409 | A hidden recipe cannot be published. |
| `recipe.concurrentChange` | 409 | The loaded recipe was changed by another write. |
| `recipe.saveFailed` | 500 | The operation did not persist. |

Application prechecks and the database uniqueness fallback return the same code
for the same conflict. No constraint names or database details are exposed.

## Validation rules

Domain validation uses top-level `validation.failed` with one or more field rules.
Use field labels from the client UI alongside these generic rule translations.

| Field code | Parameters | Meaning |
| --- | --- | --- |
| `validation.required` | none | A value is required. |
| `validation.maxLength` | `max` | Text exceeds its maximum length. |
| `validation.maxItems` | `max` | A collection exceeds its allowed count. |
| `validation.minItems` | `min` | A collection needs more elements to publish. |
| `validation.positive` | none | Number must be greater than zero. |
| `validation.duplicate` | none | Ingredient is repeated. |
| `validation.unsupportedValue` | none | Unit code is unsupported. |
| `validation.emptyId` | none | Empty GUID is not allowed. |
| `validation.invalid` | none | Generic fallback for a rule without a specific code. |

Maximum parameters are the rule's actual configured limits. For tags, the count
can refer to either incoming entries (100) or distinct nonempty tags (20).

## Request and fallback codes

| Code | Meaning |
| --- | --- |
| `request.invalid` | Invalid request; also the top-level code for automatic model-binding 400 responses. |
| `request.invalidValue` | Field-level fallback for model-binding errors such as invalid JSON value types. |
| `resource.notFound` | Generic not-found response, including unmatched routes. |
| `resource.conflict` | Generic conflict when no domain code was supplied. |
| `http.methodNotAllowed` | HTTP 405: the route does not support the requested method. |
| `http.unsupportedMediaType` | HTTP 415: the request content type is unsupported. |
| `http.error` | Other HTTP errors without a more specific code; inspect the status. |
| `error.failure` | Explicit failure without a more specific code. |
| `error.unexpected` | Unexpected server exception handled by the API middleware. |

Automatic binding errors keep ASP.NET's field paths and diagnostic messages.
Their codes are assigned without interpreting framework-generated English text.
Unhandled exceptions expose a generic code and do not disclose exception details.
Only HTTP 400 uses the automatic `request.invalid` mapping. Missing or null recipe
names in otherwise valid JSON reach domain validation and return
`validation.required` under the `name` field, for both create and update requests.

`RecipePersistenceErrorCodes` in Application owns `recipe.concurrentChange` and
`recipe.saveFailed`. Their serialized values remain unchanged. Domain owns its
business outcome codes; Infrastructure consumes the Application persistence codes.

Unit codes such as `ud`, `cucharadita` and `cucharada` are stable transport values,
not UI labels. The frontend translates their labels while sending these same
codes in either language. Recipe names and user-written contents are not translated.

Clients should keep a localized generic fallback for unknown future codes. A
code's meaning is a public contract: changing diagnostic wording does not change
the code, and a new meaning needs a new code. Domain and Common do not depend on
the UI language or translation resources. No database migration is needed.
