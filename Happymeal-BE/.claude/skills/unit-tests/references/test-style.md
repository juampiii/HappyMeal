---
name: unit-tests-test-style
description: Consult when naming tests, organizing classes, writing Arrange-Act-Assert blocks or theories.
---

## Naming

Use `[Method]_When[Condition]_Should[ExpectedResult]`:

- `HandleAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound`
- `Create_WhenNameIsEmpty_ShouldReturnFailure`
- `Name_WhenLongerThanMaxLength_ShouldHaveValidationError`

## Class Organization

No shared base class. Keep collaborators and the system under test in private readonly fields, created in the constructor. Name the system under test `_sut`.

```csharp
public class UpdateRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateRecipeFeature.Handler _sut;

    public UpdateRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateRecipeFeature.Handler(_unitOfWork);
    }
}
```

Create the SUT inside a test only when the scenario needs a special dependency.

## Arrange, Act, Assert

Use `// Arrange`, `// Act`, `// Assert` when setup is not trivial. One main behavior per test. Use `CancellationToken.None` unless the test checks token propagation; then share one local `cancellationToken`.

## Expression-bodied tests

Allowed for simple validation cases that fit in one expression:

```csharp
[Fact]
public void Validate_WhenNameIsEmpty_ShouldReturnNameFailure()
    => (ValidParams() with { Name = "" }).Validate()
        .Should().ContainSingle(f => f.PropertyName == nameof(RecipeParams.Name));
```

## Theories

Use `[Theory]` + `[InlineData]` for scalar variations. Use `[MemberData]` with a well-named static factory next to the tests for complex values. Avoid `[ClassData]` unless the data is shared by several classes.

## Test data

Start from one valid baseline (a private factory such as `ValidParams()`) and change one field with `with`. Add a builder in `Builders/` only when two or more test classes need the same graph.
