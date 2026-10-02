using System.Globalization;
using SEBlueprint.Core.Parsing;

namespace SEBlueprint.Core.Tests;

public class NumTests
{
    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("ru-RU")]
    public void Parses_dot_decimal_regardless_of_culture(string culture)
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            Assert.Equal(1.5, Num.D("1.5"));
            Assert.Equal(2.25f, Num.F(" 2.25 "));
            Assert.Equal(7, Num.I("7"));
            Assert.Equal(3, Num.D("bad", 3));
            Assert.Equal(0, Num.D(null));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }
}
