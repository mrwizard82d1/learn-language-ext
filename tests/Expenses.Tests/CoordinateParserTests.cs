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
    [InlineData("45.9")]
    [InlineData("-90.0")]
    [InlineData("90.0")]
    public void ValidLatitudeText_ParseLatitude_SuccessfullyParsed(string latitudeText)
    {
        var actual = Latitude.ParseLatitude(latitudeText);
        
        var expected = decimal.Parse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture);
        actual.Match(Succ: latitude => LangExtAssert.Equal(expected, latitude.Degrees),
            Fail: error => Assert.Fail(error.Message));
    }

    [Fact]
    public void NonNumericLatitudeText_ParseDecimal_ReportsError()
    {
        // The letter 'O' not the digit zero '0'
        const string nonNumericDecimalText = "75.3O";
        var nonNumericDecimal = DecimalParser.ParseDecimal(nonNumericDecimalText);
        
        nonNumericDecimal.Match(
            Succ: _ => 
                throw new InvalidOperationException($"Expected parse failure of '{nonNumericDecimalText}'. " 
                                                    + "Unexpectedly succeeded."),
            Fail: error => Assert.Equal($"Failed to parse decimal: '{nonNumericDecimalText}'", error));
    }

    [Fact]
    public void LatitudeOutOfRange_ParseLatitude_ReportsError()
    {
        const string outOfRangeLatitudeText = "90.00001";
        var outOfRangeLatitudeResult = Latitude.ParseLatitude(outOfRangeLatitudeText);
        
        outOfRangeLatitudeResult.Match(
            Succ: _ => 
                throw new InvalidOperationException($"Expected parse failure of '{outOfRangeLatitudeText}'. " 
                                                    + "Unexpectedly succeeded."),
            Fail: errorText => Assert.Equal($"Parsed latitude, '{outOfRangeLatitudeText}', is out of range", 
                                            errorText));
    }
}

public static class CoordinateParser
{
}

public readonly record struct Latitude
{
    public decimal Degrees { get; }

    private Latitude(decimal degrees) => Degrees = degrees;

    public static Fin<Latitude> ParseLatitude(string latitudeText) =>
        DecimalParser.ParseDecimal(latitudeText)
            .Bind(Create);

    public static Fin<Latitude> Create(decimal degrees) =>
        degrees is >= -90.0m and <= 90.0m
            ? FinSucc(new Latitude(degrees))
            : FinFail<Latitude>(Error.New($"Parsed latitude, '{degrees}', is out of range"));
}

public static class DecimalParser
{
    public static Fin<decimal> ParseDecimal(string candidateText) =>
        decimal.TryParse(candidateText,
                         NumberStyles.Float,
                         CultureInfo.InvariantCulture,
                         out var candidateLatitude)
            ? FinSucc(candidateLatitude)
            : FinFail<decimal>(Error.New($"Failed to parse decimal: '{candidateText}'"));
}
