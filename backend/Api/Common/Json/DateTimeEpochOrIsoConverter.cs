using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Common.Json;

/// <summary>
/// JSON converter that accepts either a numeric epoch milliseconds value or an ISO date string
/// and deserializes it to a DateTime (UTC).
/// </summary>
public class DateTimeEpochOrIsoConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Expect epoch milliseconds
            if (reader.TryGetInt64(out var ms))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                }
                catch
                {
                    // Fall through to default parsing
                }
            }
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrEmpty(s))
                return default;

            if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                return dt.ToUniversalTime();

            // Try parsing as long string
            if (long.TryParse(s, out var ms2))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(ms2).UtcDateTime;
                }
                catch
                {
                }
            }
        }

        // If we can't parse, return default(DateTime)
        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Serialize as ISO 8601 in UTC
        writer.WriteStringValue(value.ToUniversalTime().ToString("o"));
    }
}