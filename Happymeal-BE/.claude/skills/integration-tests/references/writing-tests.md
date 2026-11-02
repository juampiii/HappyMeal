---
name: integration-tests-writing-tests
description: Consult when authoring test classes - naming, AAA, HTTP calls, seeding, querying and assertions.
---

## Class shape

Use a primary constructor with `HappymealApiFactory`, extend `TestBase(factory)`, and mirror the feature in the folder and namespace:

```csharp
namespace Happymeal.IntegrationTests.Recipes;

public class CreateRecipeTests(HappymealApiFactory factory) : TestBase(factory)
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldReturnCreatedAndPersistRecipe()
    {
        // Arrange
        var request = new CreateRecipeRequestDto(null, "Burger", "With fries");

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<RecipeDto>(TestContext.Current.CancellationToken);
        dto!.Name.Should().Be("Burger");

        var stored = await QueryAsync(db => db.Recipes.SingleAsync(r => r.Id == dto.Id));
        stored.Description.Should().Be("With fries");
    }
}
```

## Naming

`[Method]_When[Condition]_Should[ExpectedResult]`, for example `GetByIdAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound`.

## HTTP calls

- Use `Client` from `TestBase` with `System.Net.Http.Json` (`PostAsJsonAsync`, `PutAsJsonAsync`, `GetFromJsonAsync`, `ReadFromJsonAsync`).
- Reuse the production DTOs (`CreateRecipeRequestDto`, `UpdateRecipeRequestDto`, `RecipeDto`) so contract changes break the tests at compile time.
- Pass `TestContext.Current.CancellationToken` (xUnit v3).
- Route strings are relative (`"api/recipes"`). Keep them in a small static class per feature if they repeat.

## Seeding and querying

- Each test starts from an empty database. Seed everything the scenario needs.
- Seed through the domain API, so the data is valid: `await SeedAsync(db => db.Recipes.AddAsync(Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value).AsTask());`.
- `SeedAsync` calls `SaveChangesAsync` directly, not `UnitOfWork.SaveEntitiesAsync`, so seeded entities do not create outbox rows. Use the HTTP endpoint when the test needs the event. The `AuditableInterceptor` still runs, and outside a request it stamps `CreatedBy = "system"`.
- Read the result with `QueryAsync(...)`, which uses a new scope and context, so you do not see a cached entity.

## Assertions

- Status code first: `response.StatusCode.Should().Be(HttpStatusCode.NotFound);`.
- Error bodies: deserialize to `ProblemDetails` or `ValidationProblemDetails` (`Microsoft.AspNetCore.Mvc`) and assert `Status`, and the error keys (`errors.Should().ContainKey("name")`). Do not assert full message text.
- For success, assert the fields that prove the behavior, plus the database state when persistence is the point.
