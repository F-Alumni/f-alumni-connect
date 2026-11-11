using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForumService.Api.Serialization;

/// <summary>
/// Serializes DateTime values in Vietnam time (GMT+7 / +07:00) and normalizes
/// incoming JSON timestamps to UTC for storage and query comparisons.
/// </summary>
public sealed class VietnamDateTimeJsonConverter : JsonConverter<DateTime>
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for DateTime, got {reader.TokenType}.");
        }

        var raw = reader.GetString();
        if (!string.IsNullOrWhiteSpace(raw) &&
            DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return NormalizeToUtc(raw, parsed);
        }

        var fallback = reader.GetDateTime();
        return NormalizeToUtc(null, fallback);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // SQL Server datetime2 reads back as DateTimeKind.Unspecified even though values are persisted in UTC.
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        if (utcValue > DateTime.MaxValue - VietnamOffset)
        {
            writer.WriteStringValue(new DateTimeOffset(utcValue, TimeSpan.Zero));
            return;
        }

        var vietnamTime = new DateTimeOffset(utcValue, TimeSpan.Zero).ToOffset(VietnamOffset);
        writer.WriteStringValue(vietnamTime);
    }

    private static DateTime NormalizeToUtc(string? raw, DateTime parsed)
    {
        if (parsed.Kind == DateTimeKind.Utc)
        {
            return parsed;
        }

        if (parsed.Kind == DateTimeKind.Local)
        {
            if (raw != null &&
                DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedOffset))
            {
                return parsedOffset.UtcDateTime;
            }

            return parsed.ToUniversalTime();
        }

        // Treat timestamps without a timezone designator as Vietnam local time (GMT+7).
        if (parsed.Ticks < VietnamOffset.Ticks)
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return new DateTimeOffset(parsed, VietnamOffset).UtcDateTime;
    }
}
