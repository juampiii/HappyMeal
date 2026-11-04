using System.ComponentModel.DataAnnotations;

namespace Happymeal.Infrastructure.Outbox;

public class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;

    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 5;

    [Range(typeof(TimeSpan), "00:00:10", "01:00:00")]
    public TimeSpan ClaimTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
