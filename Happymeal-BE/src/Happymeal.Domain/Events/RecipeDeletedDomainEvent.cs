using Happymeal.Domain.Base;

namespace Happymeal.Domain.Events;

public sealed record RecipeDeletedDomainEvent(Guid RecipeId) : DomainEvent;
