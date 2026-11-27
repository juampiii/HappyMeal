---
name: code-style
description: Apply the agreed Happymeal-BE C# readability conventions when writing or reviewing production code and tests. Maintain these conventions during file-by-file review. Architecture and business rules remain in their respective skills and story documents.
---

# Happymeal-BE code style

These are confirmed project conventions. Apply them to the code being changed or
reviewed. Keep unrelated formatting out of a focused review change.

## Parameters and control flow

- Always use braces for every `if` and `else` body, including single statements.
- When a parameter declaration spans multiple lines, begin the parameters on the
  line after the opening parenthesis and put one parameter on each line.

```csharp
public sealed record Command(
    Guid? Id,
    string Name,
    string? Description = null);
```

## Request-to-command mapping

- Put request DTO to Application command mappings in a static `<Entity>Mapper`
  class under `Happymeal.API/Mappers`, such as `RecipeMapper`.
- Use `MapToCommand` overloads for distinct request types. Receive the request
  first, followed by route identifiers or other information needed to build the
  command. An update identifier comes explicitly from the route.
- Controllers map to a named `command` variable, then call `sender.SendAsync`.
  Keep the field-by-field construction inside the mapper.
- Use named constructor arguments, one per line, for these mappings.
- Mappers only translate data. Keep validation and normalization in the domain.
  Do not make Application or Domain depend on API DTOs.
- Simple commands or queries built only from an identifier do not need a request
  mapper when there is no request DTO to map.

## Validation

- For nontrivial params validation, use small private methods named after the
  groups of rules they validate. Compose their results with `Concat` in
  `Validate()` so all failures are returned.
- Resolve nullable collections once locally, for example `var steps = Steps ?? [];`.
- When error paths need element indices, prefer a simple indexed loop over a LINQ
  pipeline that adds tuples only to preserve those indices. Use LINQ when it makes
  the particular validation easier to read; a loop is not inherently less idiomatic.

## Tests

- Give each test one main behavior and a name that explains its condition and
  expected result. Keep validation and name-comparison tests in their own classes,
  rather than mixing them into recipe lifecycle tests.
- Separate nontrivial setup, execution and verification with `// Arrange`,
  `// Act` and `// Assert`. Keep action results in named variables instead of
  combining calls, awaits and assertions into one expression.
- Use descriptive names such as `editingScope`, `publishingUnitOfWork`,
  `firstResponse` and `repeatedResponse`. Lay out long requests and calls so the
  scenario can be read without unpacking a dense line.
- Keep test helpers focused on shared setup; leave the behavior being tested
  visible in the test. Follow the unit/integration test skills for test mechanics.

## Keeping review notes

Record newly confirmed, reusable style preferences here as the review progresses.
Keep file-specific fixes and business decisions in their appropriate code or story
rather than generalizing them into style rules. Retain the scope of a preference
and its rationale when they affect how it should be applied.
