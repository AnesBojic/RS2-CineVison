using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CineVision.Services.Database
{
    /// <summary>SQL datetime2 Unspecified → Kind=Utc on read.</summary>
    public sealed class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeValueConverter()
            : base(
                v => NormalizeToUtc(v),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }

        internal static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public sealed class UtcNullableDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcNullableDateTimeValueConverter()
            : base(
                v => v.HasValue ? UtcDateTimeValueConverter.NormalizeToUtc(v.Value) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
