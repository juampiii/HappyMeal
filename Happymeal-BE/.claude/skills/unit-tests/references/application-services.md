---
name: unit-tests-application-services
description: Consult when testing command handlers, query handlers and domain event handlers with substituted collaborators.
---

## Command handlers: result + state + events + interactions

A thorough command handler test checks up to four things. Include a part when it is part of the behavior; skip it when it only restates the Arrange step.

1. The `Result`/`Result<T>`: success value, or error type and validation paths.
2. The entity state: read it from the instance you passed to the substitute, not through a second fetch.
3. The domain events the entity raised.
4. The collaborator calls that prove the behavior (`Received`/`DidNotReceive`).

```csharp
[Fact]
public async Task HandleAsync_WhenRecipeExists_ShouldUpdateAndRaiseRecipeUpdatedDomainEvent()
{
    // Arrange
    var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
    recipe.PullDomainEvents();
    _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
    _unitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

    // Act
    var result = await _sut.HandleAsync(new UpdateRecipeCommand(recipe.Id, "Burger XL", "Big"), CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("Burger XL");
    recipe.Name.Should().Be("Burger XL");
    recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipeUpdatedDomainEvent>();
    await _unitOfWork.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
}
```

Failure before persistence: assert the error type and that nothing was saved.

```csharp
result.Error!.Type.Should().Be(ErrorType.NotFound);
await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
```

Notes:

- Call `HandleAsync` directly. Do not go through a real `ISender` in a unit test.
- `SaveEntitiesAsync` is substituted, so the outbox staging is not exercised. Domain events stay on the entity, which is what makes part 3 possible.
- `UnitOfWork` outbox staging and the dispatcher are covered by integration tests.

## Query handlers

Assert the returned value or the `NotFound` error. Also assert that the handler did not call `SaveEntitiesAsync` when that is a risk.

## Domain event handlers

Handlers implement `IDomainEventHandler<TDomainEvent>` and return `Result`. Build the event record directly, call `HandleAsync`, and assert the `Result` plus the collaborator calls that prove the reaction:

```csharp
var result = await _sut.HandleAsync(new RecipeCreatedDomainEvent(Guid.NewGuid(), "Burger"), Guid.NewGuid(), CancellationToken.None);

result.IsSuccess.Should().BeTrue();
```

Handlers must be idempotent. When a handler writes state, add a test that calls it twice with the same event and asserts the effect happens once.

A handler that sends a command through `ISender`: substitute the sender, return a `Result` from `SendAsync`, and assert that a failure is passed through.

## Collaboration rules

Verify the collaboration that proves behavior; do not assert every internal call. If a test needs real persistence or HTTP to make sense, move it to the `integration-tests` skill.
