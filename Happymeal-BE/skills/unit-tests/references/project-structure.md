---
name: unit-tests-project-structure
description: Consult for the UnitTests folder layout, packages, global usings and namespaces.
---

## Project

`tests/Happymeal.UnitTests/Happymeal.UnitTests.csproj` references `Happymeal.Domain`, `Happymeal.Application` and `Happymeal.Infrastructure`.

| Package | Purpose |
|---------|---------|
| `xunit.v3` + `xunit.runner.visualstudio` | Test framework. Runs on Microsoft.Testing.Platform (`global.json`). |
| `NSubstitute` | Substitutes for interfaces and `TimeProvider`. |
| `AwesomeAssertions` | Fluent assertions (free fork of FluentAssertions). |

Global usings (in the csproj): `Xunit`, `AwesomeAssertions`, `NSubstitute`. Do not repeat them in test files.

`Happymeal.Domain`, `Happymeal.Application` and `Happymeal.Infrastructure` grant `InternalsVisibleTo` to `Happymeal.UnitTests`, so internal repositories and helpers can be tested.

## Folder layout

Mirror the production project and folder of the class under test:

```text
Happymeal.UnitTests/
├── Domain/
│   ├── RecipeTests.cs
│   └── Params/RecipeParamsTests.cs
├── Application/
│   └── Features/Recipes/
│       ├── Commands/CreateRecipe/CreateRecipeCommandHandlerTests.cs
│       └── DomainEventHandlers/LogRecipeLifecycleDomainEventHandlerTests.cs
├── Infrastructure/
│   └── Outbox/OutboxSerializerTests.cs
└── Builders/                     shared test data builders, only when two or more classes need them
```

Namespace = `Happymeal.UnitTests.` + folder path. One test class per production class, named `<ClassName>Tests`. For nested Feature handlers, keep the descriptive `<UseCase>CommandHandlerTests` or `<UseCase>QueryHandlerTests` class/file names (for example `CreateRecipeCommandHandlerTests` tests `CreateRecipeFeature.Handler`). Avoid naming every test class `HandlerTests`.

## Running

```bash
dotnet test --project tests/Happymeal.UnitTests
```
