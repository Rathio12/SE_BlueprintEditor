using System.Globalization;
using SEBlueprint.Core.Parsing;

namespace SEBlueprint.Core.Tests;

public class UserNumbersTests
{
    [Theory]
    [InlineData("en-US", "1,000,000", 1000000.0)]
    [InlineData("en-US", "1000000", 1000000.0)]
    [InlineData("en-US", "12.5", 12.5)]
    [InlineData("de-DE", "1.000.000", 1000000.0)]
    [InlineData("de-DE", "12,5", 12.5)]
    [InlineData("fr-FR", "1 000 000", 1000000.0)]
    [InlineData("fr-FR", "1 000 000", 1000000.0)]
    [InlineData("de-CH", "1'000'000", 1000000.0)]
    public void Parses_numbers_as_the_user_types_them(string culture, string text, double expected)
    {
        Assert.True(UserNumbers.TryParseLimit(text, new CultureInfo(culture), integer: false, out var value));
        Assert.Equal(expected, value!.Value, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("0")]
    public void Empty_or_zero_means_unlimited(string? text)
    {
        Assert.True(UserNumbers.TryParseLimit(text, new CultureInfo("en-US"), integer: true, out var value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("12.5")]
    [InlineData("1e9999")]
    public void Rejects_invalid_integer_limits_instead_of_dropping_them(string text)
    {
        Assert.False(UserNumbers.TryParseLimit(text, new CultureInfo("en-US"), integer: true, out _));
    }
}
