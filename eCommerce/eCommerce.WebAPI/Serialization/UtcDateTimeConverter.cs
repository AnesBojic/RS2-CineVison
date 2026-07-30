using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eCommerce.WebAPI.Serialization
{
    /// <summary>
    /// The API speaks UTC only. Incoming values without an explicit offset are assumed
    /// to already be UTC, and outgoing values always carry a trailing "Z" so clients can
    /// convert to their own local time instead of guessing.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        private const string OutputFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var raw = reader.GetString();

            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new JsonException("Expected an ISO 8601 date-time value.");
            }

            if (!DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                throw new JsonException($"'{raw}' is not a valid ISO 8601 date-time value.");
            }

            return parsed.UtcDateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            writer.WriteStringValue(utc.ToString(OutputFormat, CultureInfo.InvariantCulture));
        }
    }
}
