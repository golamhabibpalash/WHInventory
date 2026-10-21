using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ASPNET.BackEnd.Common.Helpers;

/// <summary>
/// Serializes UTC instant properties so the browser can convert them to the viewer's own local time.
///
/// Audit/event timestamps (CreatedAtUtc, UpdatedAtUtc, LastActivityAtUtc, SlaResponseDueAtUtc, ...)
/// are stored in UTC, but Npgsql (with EnableLegacyTimestampBehavior) reads them back as
/// <see cref="DateTimeKind.Unspecified"/> and the default serializer emits them with no offset, so
/// JavaScript reads them as browser-local — the clock mismatch users see outside the server's zone.
///
/// The naming convention in this codebase is that UTC instants end in "Utc". This modifier attaches
/// a <see cref="UtcDateTimeConverter"/> to exactly those properties, leaving business dates
/// (OrderDate, MovementDate, DeliveryDate, ...) timezone-naive so no zone can shift them by a day.
/// </summary>
public static class UtcDateTimeJsonModifier
{
    private static readonly UtcDateTimeConverter _converter = new();

    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            var name = property.Name;
            var isUtc = name.EndsWith("Utc", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("UtcDate", StringComparison.OrdinalIgnoreCase);

            if (!isUtc)
            {
                continue;
            }

            var propertyType = property.PropertyType;
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                property.CustomConverter = _converter;
            }
        }
    }

    private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}
