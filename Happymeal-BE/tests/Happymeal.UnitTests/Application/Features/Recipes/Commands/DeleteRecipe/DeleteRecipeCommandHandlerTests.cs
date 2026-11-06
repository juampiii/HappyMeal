using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Commands.DeleteRecipe;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteRecipeCommandHandler _sut;

    public DeleteRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new DeleteRecipeCommandHandler(_unitOfWork);
    }

    private static Recipe CreateRecipe()
    {
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
        recipe.PullDomainEvents();
        return recipe;
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeExists_ShouldDeleteAndRaiseRecipeDeletedDomainEvent()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        _unitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.HandleAsync(new DeleteRecipeCommand(recipe.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipeDeletedDomainEvent>();
        _unitOfWork.RecipeRepository.Received(1).Delete(recipe);
        await _unitOfWork.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeDoesNotExist_ShouldReturnNotFoundAndNotSave()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act
        var result = await _sut.HandleAsync(new DeleteRecipeCommand(id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        _unitOfWork.RecipeRepository.DidNotReceive().Delete(Arg.Any<Recipe>());
        await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ShouldReturnFailure()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);
        _unitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(new DeleteRecipeCommand(recipe.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }
}
