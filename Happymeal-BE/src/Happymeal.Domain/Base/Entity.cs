namespace Happymeal.Domain.Base;

public abstract class Entity(Guid id)
{
    public Guid Id { get; private set; } = id;
}
