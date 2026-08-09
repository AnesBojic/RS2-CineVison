using System;

namespace CineVision.Services
{
    /// <summary>
    /// Formats stored UTC instants for user-facing text (emails) in the cinema's local zone.
    /// Docker containers usually run as UTC, so <see cref="DateTime.ToLocalTime"/> is not enough.
    /// </summary>
    public static class CinemaDateTime
    {
        private static readonly TimeZoneInfo Zone = ResolveZone();

        private static TimeZoneInfo ResolveZone()
        {
            foreach (var id in new[]
                     {
                         "Europe/Sarajevo",
                         "Europe/Belgrade",
                         "Central European Standard Time"
                     })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Local;
        }

        /// <summary>Normalizes an API/DB instant to UTC, then formats in cinema local time.</summary>
        public static string FormatLocal(DateTime value, string format = "yyyy-MM-dd HH:mm")
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
            return local.ToString(format);
        }
    }
}
