using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Queries.GetRecipeById;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GetRecipeByIdFeature.Handler _sut;

    public GetRecipeByIdQueryHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new GetRecipeByIdFeature.Handler(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeExists_ShouldReturnDto()
    {
        // Arrange
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", "With fries")).Value;
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        var result = await _sut.HandleAsync(new GetRecipeByIdFeature.Query(recipe.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(recipe.Id);
        result.Value.Name.Should().Be("Burger");
        result.Value.Description.Should().Be("With fries");
        await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act
        var result = await _sut.HandleAsync(new GetRecipeByIdFeature.Query(id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
