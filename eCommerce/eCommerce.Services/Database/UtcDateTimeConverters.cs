using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace eCommerce.Services.Database
{
    /// <summary>
    /// SQL Server returns <see cref="DateTimeKind.Unspecified"/>. Treat every persisted
    /// instant as UTC so comparisons with <see cref="DateTime.UtcNow"/> stay correct.
    /// </summary>
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
