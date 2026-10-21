namespace Application.Common.Services.BusinessClockManager;

/// <summary>
/// The single source of "now" for business dates (OrderDate, MovementDate, DeliveryDate, document
/// number dates, dashboard day boundaries). Business dates are calendar dates in the configured
/// business timezone (appsettings.json -> TimeZoneId), not the server's OS clock and not the
/// viewer's clock, so reporting and document numbering stay stable for every user worldwide.
/// Audit/event timestamps (CreatedAtUtc, UpdatedAtUtc, ...) are instants and must keep using
/// <see cref="DateTime.UtcNow"/> directly, or they are converted at the edge.
/// </summary>
public interface IBusinessClock
{
    /// <summary>The business timezone this deployment books business dates in.</summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>Current instant in the business timezone, with a UTC offset.</summary>
    DateTimeOffset NowOffset { get; }

    /// <summary>Current wall-clock time in the business timezone (Unspecified kind).</summary>
    DateTime Now { get; }

    /// <summary>Today's calendar date in the business timezone.</summary>
    DateTime Today { get; }

    /// <summary>Converts a UTC instant to the business timezone.</summary>
    DateTime ToBusinessTime(DateTime utc);

    /// <summary>UTC instant for the given business wall-clock time.</summary>
    DateTime ToUtc(DateTime businessTime);
}
