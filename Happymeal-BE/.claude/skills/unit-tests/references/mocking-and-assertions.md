---
name: unit-tests-mocking-and-assertions
description: Consult when substituting collaborators with NSubstitute, verifying calls, or asserting Result and Error with AwesomeAssertions.
---

## NSubstitute

Substitute interfaces and framework collaborators. Never substitute domain entities: build them with `Recipe.Create(...)`.

`IUnitOfWork` exposes repositories as properties. NSubstitute returns a recursive substitute for them, so configure the repository through the unit of work:

```csharp
_unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
_unitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);
```

- `Arg.Any<T>()` for irrelevant arguments, `Arg.Is<T>(x => ...)` for the part that proves behavior.
- Verify the collaboration that proves behavior, not every call:

```csharp
_unitOfWork.RecipeRepository.Received(1).Add(Arg.Is<Recipe>(r => r.Name == "Burger"));
await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
```

- `ISender`: substitute it when the SUT sends commands (for example a domain event handler). Return a `Result` from `SendAsync` and assert how the SUT maps it.
- `ILogger<T>`: create `Substitute.For<ILogger<T>>()` when needed. Do not verify logs unless logging is the contract under test.
- `TimeProvider`: `var timeProvider = Substitute.For<TimeProvider>(); timeProvider.GetUtcNow().Returns(now);`. Never compare against a fresh `DateTimeOffset.UtcNow` in the assertion.
- Exceptions: use `.Throws(...)`/`.ThrowsAsync(...)` (from `NSubstitute.ExceptionExtensions`) only when the SUT handles or surfaces the exception.

## AwesomeAssertions

Same API as FluentAssertions 7. `using AwesomeAssertions;` is global. Prefer one clear assertion per fact; use `using (new AssertionScope())` when several facts describe one outcome.

## Asserting `Result` and `Result<T>`

State is public. Check success first, then the value or the error:

```csharp
result.IsSuccess.Should().BeTrue();
result.Value.Name.Should().Be("Burger");
```

```csharp
result.IsFailure.Should().BeTrue();
result.Error!.Type.Should().Be(ErrorType.NotFound);
```

```csharp
result.Error!.Type.Should().Be(ErrorType.Validation);
result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
```

Validation paths are C# names (`Name`) below the API. Only HTTP responses use JSON names (`name`).

`Value` throws on a failed result. That gives a clear message when a success test fails, so asserting `IsSuccess` first is optional but reads better.
