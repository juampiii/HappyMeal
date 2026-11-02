namespace Happymeal.Domain.Base;

/// <summary>Who created and last modified the entity, and when. Infrastructure fills these values when it saves.</summary>
public interface IAuditable
{
    public const int UserMaxLength = 256;

    DateTimeOffset CreatedOn { get; }
    string CreatedBy { get; }
    DateTimeOffset? ModifiedOn { get; }
    string? ModifiedBy { get; }
}
