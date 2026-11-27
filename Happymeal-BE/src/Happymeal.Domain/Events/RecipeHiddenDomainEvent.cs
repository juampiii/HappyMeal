using Happymeal.Domain.Base;
namespace Happymeal.Domain.Events;
public sealed record RecipeHiddenDomainEvent(Guid RecipeId) : DomainEvent;
