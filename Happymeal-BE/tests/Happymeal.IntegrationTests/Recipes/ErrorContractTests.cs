using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Common.Messaging;
using Happymeal.Application.Features.Recipes;
using Happymeal.Application.Features.Recipes.Queries.GetRecipeById;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Happymeal.IntegrationTests.Recipes;

public class ErrorContractTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    public async Task CreateAsync_WhenNameIsMissingOrNull_ShouldReturnRequiredValidationCode(string json)
    {
        // Arrange
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("api/recipes", content, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("validation.failed");
        problem.GetProperty("validationErrors").GetProperty("name")[0].GetProperty("code")
            .GetString().Should().Be("validation.required");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    public async Task UpdateAsync_WhenNameIsMissingOrNull_ShouldReturnRequiredCodeAndKeepName(string json)
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Soup")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PutAsync($"api/recipes/{recipe.Id}", content, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("validation.failed");
        problem.GetProperty("validationErrors").GetProperty("name")[0].GetProperty("code")
            .GetString().Should().Be("validation.required");
        var saved = await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{recipe.Id}", CancellationToken);
        saved!.Name.Should().Be("Soup");
    }

    [Fact]
    public async Task PatchAsync_WhenMethodIsUnsupported_ShouldReturnMethodCode()
    {
        // Act
        var response = await Client.PatchAsync("api/recipes", null, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("http.methodNotAllowed");
    }

    [Fact]
    public async Task CreateAsync_WhenMediaTypeIsUnsupported_ShouldReturnMediaTypeCode()
    {
        // Arrange
        using var content = new StringContent("Soup", Encoding.UTF8, "text/plain");

        // Act
        var response = await Client.PostAsync("api/recipes", content, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("http.unsupportedMediaType");
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ShouldReturnCodesParametersAndExistingMessages()
    {
        // Arrange
        var request = new CreateRecipeRequestDto(
            Id: null,
            Name: "Soup",
            Ingredients: [new("Salt", Unit: "unknown")],
            Steps: [""],
            Tags: [new string('t', RecipeTag.NameMaxLength + 1)]);

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("validation.failed");
        var fields = problem.GetProperty("validationErrors");
        fields.GetProperty("ingredients[0].unit")[0].GetProperty("code").GetString()
            .Should().Be("validation.unsupportedValue");
        fields.GetProperty("steps[0]")[0].GetProperty("code").GetString().Should().Be("validation.required");
        var tagError = fields.GetProperty("tags[0]")[0];
        tagError.GetProperty("code").GetString().Should().Be("validation.maxLength");
        tagError.GetProperty("parameters").GetProperty("max").GetInt32().Should().Be(RecipeTag.NameMaxLength);
        problem.GetProperty("errors").GetProperty("tags[0]").GetArrayLength().Should().Be(1);
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeDoesNotExist_ShouldReturnStableNotFoundCode()
    {
        // Act
        var response = await Client.GetAsync($"api/recipes/{Guid.CreateVersion7()}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("recipe.notFound");
        problem.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(true, "recipe.idAlreadyExists")]
    [InlineData(false, "recipe.nameAlreadyExists")]
    public async Task CreateAsync_WhenRecipeAlreadyExists_ShouldReturnSpecificConflictCode(
        bool duplicateId,
        string expectedCode)
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Soup")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());
        var request = new CreateRecipeRequestDto(
            Id: duplicateId ? recipe.Id : null,
            Name: duplicateId ? "Salad" : "Soup");

        // Act
        var response = await Client.PostAsJsonAsync("api/recipes", request, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(expectedCode);
    }

    [Fact]
    public async Task PublishAsync_WhenDraftIsIncomplete_ShouldReturnMinimumCollectionParameters()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Soup")).Value;
        await SeedAsync(db => db.Recipes.AddAsync(recipe, CancellationToken).AsTask());

        // Act
        var response = await Client.PostAsync($"api/recipes/{recipe.Id}/publish", null, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var field = problem.GetProperty("validationErrors").GetProperty("ingredients")[0];
        field.GetProperty("code").GetString().Should().Be("validation.minItems");
        field.GetProperty("parameters").GetProperty("min").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WhenJsonHasWrongValueType_ShouldReturnBindingCodes()
    {
        // Arrange
        using var content = new StringContent("""{"name":"Soup","servings":"invalid"}""", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("api/recipes", content, CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("request.invalid");
        var fields = problem.GetProperty("validationErrors").EnumerateObject().ToArray();
        fields.Should().NotBeEmpty();
        foreach (var field in fields)
        {
            field.Value[0].GetProperty("code").GetString().Should().Be("request.invalidValue");
        }
    }

    [Fact]
    public async Task GetByIdAsync_WhenHandlerThrows_ShouldReturnGenericCodeWithoutInternalDetails()
    {
        // Arrange
        await using var failingFactory = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQueryHandler<GetRecipeByIdFeature.Query, RecipeDto>>();
            services.AddScoped<IQueryHandler<GetRecipeByIdFeature.Query, RecipeDto>, FailingQueryHandler>();
        }));
        using var failingClient = failingFactory.CreateClient();

        // Act
        var response = await failingClient.GetAsync($"api/recipes/{Guid.CreateVersion7()}", CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        problem.GetProperty("code").GetString().Should().Be("error.unexpected");
        problem.GetRawText().Should().NotContain("Sensitive database internals");
    }

    private sealed class FailingQueryHandler : IQueryHandler<GetRecipeByIdFeature.Query, RecipeDto>
    {
        public Task<Result<RecipeDto>> HandleAsync(
            GetRecipeByIdFeature.Query query,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Sensitive database internals");
        }
    }
}
