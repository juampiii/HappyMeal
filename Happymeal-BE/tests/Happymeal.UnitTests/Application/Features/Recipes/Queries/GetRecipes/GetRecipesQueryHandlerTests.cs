using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Queries.GetRecipes;
using Happymeal.Domain;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GetRecipesQueryHandler _sut;

    public GetRecipesQueryHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new GetRecipesQueryHandler(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenRecipesExist_ShouldReturnAllAsDtos()
    {
        // Arrange
        var burger = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", "With fries")).Value;
        var salad = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Salad", null)).Value;
        _unitOfWork.RecipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Recipe> { burger, salad });

        // Act
        var result = await _sut.HandleAsync(new GetRecipesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(r => r.Id).Should().Equal(burger.Id, salad.Id);
        result.Value.Select(r => r.Name).Should().Equal("Burger", "Salad");
    }

    [Fact]
    public async Task HandleAsync_WhenThereAreNoRecipes_ShouldReturnEmptyList()
    {
        // Arrange
        _unitOfWork.RecipeRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Recipe>());

        // Act
        var result = await _sut.HandleAsync(new GetRecipesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
