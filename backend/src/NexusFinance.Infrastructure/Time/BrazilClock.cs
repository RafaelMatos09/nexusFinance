using NexusFinance.Application.Abstractions;

namespace NexusFinance.Infrastructure.Time;

public sealed class BrazilClock : IClock
{
    public DateOnly Today
    {
        get
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        }
    }
}
