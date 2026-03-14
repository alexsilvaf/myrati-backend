using System.Globalization;
using Myrati.Application.Common;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ApplicationTimeTests
{
    [Fact]
    public void LocalDate_UsesApplicationTimezoneInsteadOfUtcCalendarDay()
    {
        var instant = new DateTimeOffset(2026, 3, 14, 0, 11, 0, TimeSpan.Zero);

        var localDate = ApplicationTime.LocalDate(instant);

        Assert.Equal(new DateOnly(2026, 3, 13), localDate);
    }

    [Fact]
    public void FormatLocal_UsesBrasiliaTimeForDisplayStrings()
    {
        var instant = new DateTimeOffset(2026, 3, 14, 0, 11, 0, TimeSpan.Zero);

        var formatted = ApplicationTime.FormatLocal(instant, "dd MMM yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR"));

        Assert.Equal("13 mar. 2026 às 21:11", formatted);
    }
}
