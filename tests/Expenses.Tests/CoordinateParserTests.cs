using System.Globalization;

using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Expenses.Tests;

public class CoordinateParserTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
    
    [Theory]
    [InlineData("45.9", "45.9")]
    [InlineData("-90.0", "-90.0")]
    [InlineData("90.0", "90.0")]
    public void ValidLatitudeText_ParseCoordinate_SuccessfullyParsed(string latitudeText, string expectedLatitude)
    {
        var actualLatitude = CoordinateParser.ParseLat(latitudeText);
       
        LangExtAssert.Equal(
            FinSucc(decimal.Parse(expectedLatitude, NumberStyles.Float, CultureInfo.InvariantCulture)), 
            actualLatitude);
    }
}

public record CoordinateParser(decimal Value)
{
    public static Fin<decimal> ParseLat(string latitudeText) =>
        decimal.TryParse(latitudeText,
                         NumberStyles.Float,
                         CultureInfo.InvariantCulture,
                         out var candidateLatitude)
            ? FinSucc(candidateLatitude)
            : FinFail<decimal>(Error.New($"Failed to parse coordinate {latitudeText}"));
}
