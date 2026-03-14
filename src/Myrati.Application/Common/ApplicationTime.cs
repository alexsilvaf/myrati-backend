using System.Globalization;

namespace Myrati.Application.Common;

public static class ApplicationTime
{
    public static CultureInfo PortugueseBrazil { get; } = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly Lazy<TimeZoneInfo> TimeZoneInfoLazy = new(ResolveTimeZoneInfo);

    public static TimeZoneInfo TimeZone => TimeZoneInfoLazy.Value;

    public static DateTimeOffset LocalNow() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public static DateOnly LocalToday() => LocalDate(DateTimeOffset.UtcNow);

    public static DateOnly LocalDate(DateTimeOffset value) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, TimeZone).DateTime);

    public static string FormatLocalNow(string format, IFormatProvider? provider = null) =>
        FormatLocal(DateTimeOffset.UtcNow, format, provider);

    public static string FormatLocal(DateTimeOffset value, string format, IFormatProvider? provider = null) =>
        TimeZoneInfo.ConvertTime(value, TimeZone).ToString(format, provider ?? PortugueseBrazil);

    private static TimeZoneInfo ResolveTimeZoneInfo()
    {
        var configuredTimeZone = Environment.GetEnvironmentVariable("MYRATI_APP_TIMEZONE")?.Trim();
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredTimeZone))
        {
            candidates.Add(configuredTimeZone);
        }

        candidates.Add("America/Sao_Paulo");
        candidates.Add("E. South America Standard Time");

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
