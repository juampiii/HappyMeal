---
name: unit-tests-domain-and-validation
description: Consult when testing entity factories and mutators, params validation (IValidatable), or domain events.
---

## Entities

Create entities through their public API (`Recipe.Create(...)`, `recipe.Update(...)`). Never use reflection or `Activator.CreateInstance` to bypass it. If an entity cannot be built through its API, fix the API when that is in scope, or leave a `// TODO`.

Assert the `Result<T>` and, when observable, the entity state:

```csharp
var result = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("  Burger ", "  "));

result.IsSuccess.Should().BeTrue();
result.Value.Name.Should().Be("Burger");
result.Value.Description.Should().BeNull();
```

Invalid input returns a failure, not an exception:

```csharp
var result = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("", null));

result.Error!.Type.Should().Be(ErrorType.Validation);
result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
```

Paths in the domain are C# names (`Name`). The camelCase conversion happens in the API, so do not expect `name` here.

A failed `Update` must leave the entity untouched and raise no event:

```csharp
var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
recipe.PullDomainEvents();

var result = recipe.Update(new RecipeParams("", null));

result.IsFailure.Should().BeTrue();
recipe.Name.Should().Be("Burger");
recipe.GetDomainEvents().Should().BeEmpty();
```

## Domain events

Inspect `GetDomainEvents()` right after the action. Assert type and relevant fields, not the full object:

```csharp
var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;

var recipeCreated = recipe.GetDomainEvents().Should().ContainSingle()
    .Which.Should().BeOfType<RecipeCreatedDomainEvent>().Subject;
recipeCreated.RecipeId.Should().Be(recipe.Id);
```

When the arrange step creates an entity and the test is about a later action, call `recipe.PullDomainEvents()` after creation. Otherwise the `RecipeCreatedDomainEvent` is still in the list.

## Params validation (`IValidatable`)

Test the rules on the params record directly. Keep a valid baseline and change one field with `with`:

```csharp
public class RecipeParamsTests
{
    private static RecipeParams ValidParams() => new("Burger", "With fries");

    [Fact]
    public void Validate_WhenNameIsLongerThanMaxLength_ShouldReturnNameFailure()
        => (ValidParams() with { Name = new string('a', Recipe.NameMaxLength + 1) }).Validate()
            .Should().ContainSingle(f => f.PropertyName == nameof(RecipeParams.Name));

    [Fact]
    public void Validate_WhenParamsAreValid_ShouldReturnNoFailures()
        => ValidParams().Validate().Should().BeEmpty();

    [Fact]
    public void Validate_WhenSeveralFieldsAreInvalid_ShouldReturnAllFailures()
        => new RecipeParams("", new string('a', Recipe.DescriptionMaxLength + 1)).Validate()
            .Select(f => f.PropertyName).Should().BeEquivalentTo([nameof(RecipeParams.Name), nameof(RecipeParams.Description)]);
}
```

- Use the entity constants (`Recipe.NameMaxLength`) for boundaries, not magic numbers.
- Assert property names, not message text.
- Test each rule here. In entity tests only check that a failure is returned and the state did not change.
- `Validate()` runs on the raw input. Normalization (trim, empty description to `null`) happens in the entity before validation, so cover it in entity tests.
