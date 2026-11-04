namespace Happymeal.Application.Common.Interfaces;

/// <summary>Identifies who runs the current operation. Used for audit fields.</summary>
public interface ICurrentUser
{
    public const string Anonymous = "anonymous";
    public const string System = "system";

    /// <summary>The user id from the token, <see cref="Anonymous"/> for an unauthenticated request, or <see cref="System"/> outside a request.</summary>
    string Id { get; }
}
