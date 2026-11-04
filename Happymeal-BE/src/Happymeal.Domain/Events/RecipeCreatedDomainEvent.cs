using Happymeal.Domain.Base;

namespace Happymeal.Domain.Events;

public sealed record RecipeCreatedDomainEvent(Guid RecipeId, string Name) : DomainEvent;
