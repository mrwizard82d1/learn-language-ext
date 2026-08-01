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
    [InlineData("62.1")] // The letter 'O' not the digit zero '0'
    [InlineData("-135.52")]
    public void NumericLatitudeText_ParseDecimal_ReportsError(string numericText)
    {
        var parseDecimalResult = DecimalParser.ParseDecimal(numericText);

        var expectedDecimal = decimal.Parse(numericText);
        parseDecimalResult.Match(
            Succ: _ => Assert.Equal(expectedDecimal, parseDecimalResult),
            Fail: error => Assert.Equal($"Failed to parse decimal: '{numericText}'", error.Message));
    }
    
    [Theory]
    [InlineData("75.3O")] // The letter 'O' not the digit zero '0'
    [InlineData("-83.4f")]
    public void NonNumericLatitudeText_ParseDecimal_ReportsError(string nonNumericText)
    {
        var parseDecimalResult = DecimalParser.ParseDecimal(nonNumericText);
        
        parseDecimalResult.Match(
            Succ: _ => 
                throw new InvalidOperationException($"Expected parse failure of '{nonNumericText}'. " 
                                                    + "Unexpectedly succeeded."),
            Fail: error => Assert.Equal($"Failed to parse decimal: '{nonNumericText}'", error.Message));
    }

    [Theory]
    [InlineData("45.9")]
    [InlineData("-41.7")]
    [InlineData("-90.0")]
    [InlineData("90.0")]
    public void ValidLatitudeText_ParseLatitude_SuccessfullyParsed(string latitudeText)
    {
        var actual = Latitude.ParseLatitude(latitudeText);

        var expected = decimal.Parse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture);
        actual.Match(Succ: latitude => Assert.Equal(expected, latitude.Degrees),
                     Fail: error => Assert.Fail(error.Message));
    }

    [Fact]
    public void LatitudeOutOfRange_ParseLatitude_ReportsError()
    {
        const string outOfRangeLatitudeText = "90.00001";
        var outOfRangeLatitudeResult = Latitude.ParseLatitude(outOfRangeLatitudeText);
        
        outOfRangeLatitudeResult.Match(
            Succ: _ => Assert.Fail($"Expected failure but successfully parsed {outOfRangeLatitudeText}"),
            Fail: error => Assert.Contains("is out of range", error.Message));
    }
    
    [Theory]
    [InlineData("165.0")]
    [InlineData("-138.5")]
    [InlineData("-180.0")]
    [InlineData("180.0")]
    public void ValidLongitudeText_ParseLongitude_SuccessfullyParsed(string longitudeText)
    {
        var actual = Longitude.ParseLongitude(longitudeText);

        var expected = decimal.Parse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture);
        actual.Match(Succ: longitude => Assert.Equal(expected, longitude.Degrees),
                     Fail: error => Assert.Fail(error.Message));
    }

    [Fact]
    public void LongitudeOutOfRange_ParseLongitude_ReportsError()
    {
        const string outOfRangeLongitudeText = "-180.00001";
        var outOfRangeLongitudeResult = Longitude.ParseLongitude(outOfRangeLongitudeText);
        
        outOfRangeLongitudeResult.Match(
            Succ: _ => Assert.Fail($"Expected failure but successfully parsed {outOfRangeLongitudeText}"),
            Fail: error => Assert.Contains("is out of range", error.Message));
    }
}

public static class GeoCoordinateParser
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

public readonly record struct Longitude
{
    public decimal Degrees { get; }

    private Longitude(decimal degrees) => Degrees = degrees;

    public static Fin<Longitude> ParseLongitude(string longitudeText) =>
        DecimalParser.ParseDecimal(longitudeText)
                     .Bind(Create);

    public static Fin<Longitude> Create(decimal degrees) =>
        degrees is >= -180.0m and <= 180.0m
            ? FinSucc(new Longitude(degrees))
            : FinFail<Longitude>(Error.New($"Parsed longitude, '{degrees}', is out of range"));
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
