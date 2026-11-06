using Happymeal.Common.Results;
using Happymeal.Domain.Events;
using Happymeal.Infrastructure.Outbox;

namespace Happymeal.UnitTests.Infrastructure.Outbox;

public class OutboxSerializerTests
{
    [Fact]
    public void Serialize_ShouldUseCamelCaseProperties()
    {
        var payload = OutboxSerializer.Serialize(new RecipeCreatedDomainEvent(Guid.CreateVersion7(), "Burger"));

        payload.Should().Contain("\"recipeId\"").And.Contain("\"name\"");
    }

    [Fact]
    public void Deserialize_WhenPayloadComesFromSerialize_ShouldReturnEqualEvent()
    {
        // Arrange
        var domainEvent = new RecipeCreatedDomainEvent(Guid.CreateVersion7(), "Burger");
        var payload = OutboxSerializer.Serialize(domainEvent);

        // Act
        var result = OutboxSerializer.Deserialize(payload, typeof(RecipeCreatedDomainEvent));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(domainEvent);
    }

    [Fact]
    public void Deserialize_WhenPayloadIsNotJson_ShouldReturnFailure()
    {
        var result = OutboxSerializer.Deserialize("not json", typeof(RecipeCreatedDomainEvent));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Deserialize_WhenPayloadIsJsonNull_ShouldReturnFailure()
    {
        var result = OutboxSerializer.Deserialize("null", typeof(RecipeCreatedDomainEvent));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }
}
