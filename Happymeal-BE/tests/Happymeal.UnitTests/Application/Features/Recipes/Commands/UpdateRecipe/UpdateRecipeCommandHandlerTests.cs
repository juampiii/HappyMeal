using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateRecipeCommandHandler _sut;

    public UpdateRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateRecipeCommandHandler(_unitOfWork);
    }

    private static Recipe CreateRecipe()
    {
        var recipe = Recipe.Create(Guid.CreateVersion7(), new RecipeParams("Burger", null)).Value;
        recipe.PullDomainEvents();
        return recipe;
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeExists_ShouldUpdateAndRaiseRecipeUpdatedDomainEvent()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        var result = await _sut.HandleAsync(new UpdateRecipeCommand(recipe.Id, "Burger XL", "Big"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Burger XL");
        result.Value.Description.Should().Be("Big");
        recipe.Name.Should().Be("Burger XL");
        recipe.GetDomainEvents().Should().ContainSingle().Which.Should().BeOfType<RecipeUpdatedDomainEvent>();
        await _unitOfWork.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeDoesNotExist_ShouldReturnNotFoundAndNotSave()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.GetAsync(id, Arg.Any<CancellationToken>()).Returns((Recipe?)null);

        // Act
        var result = await _sut.HandleAsync(new UpdateRecipeCommand(id, "Burger", null), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNameIsEmpty_ShouldReturnValidationErrorAndNotSave()
    {
        // Arrange
        var recipe = CreateRecipe();
        _unitOfWork.RecipeRepository.GetAsync(recipe.Id, Arg.Any<CancellationToken>()).Returns(recipe);

        // Act
        var result = await _sut.HandleAsync(new UpdateRecipeCommand(recipe.Id, "", null), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
        recipe.Name.Should().Be("Burger");
        recipe.GetDomainEvents().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }
}
