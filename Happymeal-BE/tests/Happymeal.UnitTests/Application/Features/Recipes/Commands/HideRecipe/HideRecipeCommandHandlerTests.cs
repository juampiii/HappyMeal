using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Commands.HideRecipe;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Commands.HideRecipe;

public class HideRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HideRecipeFeature.Handler _sut;

    public HideRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(true));
        _sut = new HideRecipeFeature.Handler(_unitOfWork);
    }

    private static Recipe CreateRecipe()
    {
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
        recipe.PullDomainEvents();
        return recipe;
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeExists_ShouldDeleteAndRaiseRecipeHiddenDomainEvent()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(true));

        // Act
        var result = await _sut.HandleAsync(new HideRecipeFeature.Command(recipe.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipeHiddenDomainEvent>();
        recipe.IsHidden.Should().BeTrue();
        await _unitOfWork.Received(1).TrySaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeDoesNotExist_ShouldReturnNotFoundAndNotSave()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act
        var result = await _sut.HandleAsync(new HideRecipeFeature.Command(id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);

        await _unitOfWork.DidNotReceive().TrySaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ShouldReturnFailure()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(false));

        // Act
        var result = await _sut.HandleAsync(new HideRecipeFeature.Command(recipe.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }
}
