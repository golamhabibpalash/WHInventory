using Application.Common.Services.BusinessClockManager;

namespace ASPNET.BackEnd.Common.Services;

public class BusinessClock : IBusinessClock
{
    private readonly ILogger<BusinessClock> _logger;

    public BusinessClock(IConfiguration configuration, ILogger<BusinessClock> logger)
    {
        _logger = logger;

        var timeZoneId = configuration.GetValue<string>("TimeZoneId");
        TimeZone = ResolveTimeZone(timeZoneId);
    }

    public TimeZoneInfo TimeZone { get; }

    public DateTimeOffset NowOffset => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    public DateTime Today => Now.Date;

    public DateTime ToBusinessTime(DateTime utc)
    {
        var utcValue = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, TimeZone);
    }

    public DateTime ToUtc(DateTime businessTime)
    {
        var unspecified = businessTime.Kind == DateTimeKind.Unspecified
            ? businessTime
            : DateTime.SpecifyKind(businessTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, TimeZone);
    }

    private TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("TimeZoneId '{TimeZoneId}' was not found on this host; falling back to UTC.", timeZoneId);
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogWarning("TimeZoneId '{TimeZoneId}' is invalid on this host; falling back to UTC.", timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
