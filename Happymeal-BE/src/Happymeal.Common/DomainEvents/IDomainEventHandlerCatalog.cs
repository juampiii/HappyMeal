namespace Happymeal.Common.DomainEvents;

/// <summary>Lists the handler types registered for each domain event type, without resolving them.</summary>
public interface IDomainEventHandlerCatalog
{
    IReadOnlyCollection<Type> GetHandlerTypes(Type domainEventType);
}

internal sealed class DomainEventHandlerCatalog : IDomainEventHandlerCatalog
{
    private readonly Dictionary<Type, List<Type>> _handlersByEventType = [];

    public IReadOnlyCollection<Type> GetHandlerTypes(Type domainEventType)
    {
        return _handlersByEventType.TryGetValue(domainEventType, out var handlerTypes)
            ? handlerTypes.AsReadOnly()
            : [];
    }

    internal void Add(Type domainEventType, Type handlerType)
    {
        if (!_handlersByEventType.TryGetValue(domainEventType, out var handlerTypes))
        {
            handlerTypes = [];
            _handlersByEventType[domainEventType] = handlerTypes;
        }

        if (!handlerTypes.Contains(handlerType))
        {
            handlerTypes.Add(handlerType);
        }
    }
}
