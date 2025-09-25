namespace Brainy.WebApi.Configurations;

public class RateLimiterConfiguration
{
    public int QueueLimit { get; init; }
    public int TokenLimit { get; init; }
    public int TokensPerPeriod { get; init; }
    public int ReplenishmentPeriodInSeconds { get; init; }
}
