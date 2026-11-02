using Happymeal.Domain.Base;

namespace Happymeal.Domain.Events;

public sealed record RecipeUpdatedDomainEvent(Guid RecipeId, string Name) : DomainEvent;
