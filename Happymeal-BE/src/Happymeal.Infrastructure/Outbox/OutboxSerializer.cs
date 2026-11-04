using System.Text.Json;
using System.Text.Json.Serialization;
using Happymeal.Common.Results;
using Happymeal.Domain.Base;

namespace Happymeal.Infrastructure.Outbox;

internal static class OutboxSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonSerializerOptions);
    }

    public static Result<DomainEvent> Deserialize(string payload, Type eventType)
    {
        try
        {
            var domainEvent = (DomainEvent?)JsonSerializer.Deserialize(payload, eventType, JsonSerializerOptions);
            return domainEvent is not null
                ? Result.Success(domainEvent)
                : Error.Failure("Deserialization returned null.");
        }
        catch (Exception exception)
        {
            return Error.Failure(exception.Message);
        }
    }
}
