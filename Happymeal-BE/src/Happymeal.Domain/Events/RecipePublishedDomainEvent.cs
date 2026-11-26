using Happymeal.Domain.Base;
namespace Happymeal.Domain.Events;
public sealed record RecipePublishedDomainEvent(Guid RecipeId, int Version) : DomainEvent;
