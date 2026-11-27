using Happymeal.Application.Common.Interfaces;
using Happymeal.Application.Features.Recipes.Commands.CreateRecipe;
using Happymeal.Common.Results;
using Happymeal.Domain;
using Happymeal.Domain.Events;
using Happymeal.Domain.Params;

namespace Happymeal.UnitTests.Application.Features.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateRecipeFeature.Handler _sut;

    public CreateRecipeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(true));
        _sut = new CreateRecipeFeature.Handler(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasId_ShouldCreateRecipeWithThatId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(true));

        // Act
        var result = await _sut.HandleAsync(new CreateRecipeFeature.Command(id, "Burger", "With fries"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.Name.Should().Be("Burger");
        result.Value.Description.Should().Be("With fries");
        _unitOfWork.RecipeRepository.Received(1).Add(Arg.Is<Recipe>(r =>
            r.Id == id
            && r.GetDomainEvents().Count == 1
            && r.GetDomainEvents().First() is RecipeCreatedDomainEvent));
        await _unitOfWork.Received(1).TrySaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasNoId_ShouldGenerateOne()
    {
        // Arrange
        _unitOfWork.RecipeRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(true));

        // Act
        var result = await _sut.HandleAsync(new CreateRecipeFeature.Command(null, "Burger", null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        _unitOfWork.RecipeRepository.Received(1).Add(Arg.Is<Recipe>(r => r.Id == result.Value.Id));
    }

    [Fact]
    public async Task HandleAsync_WhenNameIsEmpty_ShouldReturnValidationErrorAndNotSave()
    {
        // Act
        var result = await _sut.HandleAsync(new CreateRecipeFeature.Command(null, "", null), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.ValidationErrors.Should().ContainKey(nameof(RecipeParams.Name));
        _unitOfWork.RecipeRepository.DidNotReceive().Add(Arg.Any<Recipe>());
        await _unitOfWork.DidNotReceive().TrySaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRecipeAlreadyExists_ShouldReturnConflictAndNotSave()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _unitOfWork.RecipeRepository.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.HandleAsync(new CreateRecipeFeature.Command(id, "Burger", null), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        _unitOfWork.RecipeRepository.DidNotReceive().Add(Arg.Any<Recipe>());
        await _unitOfWork.DidNotReceive().TrySaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSaveReturnsFalse_ShouldReturnFailure()
    {
        // Arrange
        _unitOfWork.RecipeRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.TrySaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(false));

        // Act
        var result = await _sut.HandleAsync(new CreateRecipeFeature.Command(null, "Burger", null), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }
}
