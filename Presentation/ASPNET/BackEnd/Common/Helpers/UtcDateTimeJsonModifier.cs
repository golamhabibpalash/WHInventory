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
/// a converter to exactly those properties, leaving business dates (OrderDate, MovementDate,
/// DeliveryDate, ...) timezone-naive so no zone can shift them by a day.
/// </summary>
public static class UtcDateTimeJsonModifier
{
    private static readonly UtcDateTimeConverterFactory _converter = new();

    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (!property.Name.EndsWith("Utc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var propertyType = property.PropertyType;
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                // A factory (not a JsonConverter<DateTime>) is required so the converter can also be
                // used for DateTime? properties; binding a JsonConverter<DateTime> to a nullable
                // property makes serialization throw at runtime.
                property.CustomConverter = _converter;
            }
        }
    }

    private sealed class UtcDateTimeConverterFactory : JsonConverterFactory
    {
        private readonly UtcDateTimeConverter _converter = new();
        private readonly NullableUtcDateTimeConverter _nullableConverter = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(DateTime) || typeToConvert == typeof(DateTime?);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return typeToConvert == typeof(DateTime?) ? _nullableConverter : _converter;
        }
    }

    private sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            UtcDateTimeConverter.WriteUtc(writer, value.Value);
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
            WriteUtc(writer, value);
        }

        internal static void WriteUtc(Utf8JsonWriter writer, DateTime value)
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
