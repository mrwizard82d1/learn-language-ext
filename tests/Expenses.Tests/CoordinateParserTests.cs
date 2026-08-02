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
    [InlineData("62.1")]
    [InlineData("-135.52")]
    public void NumericText_ParseDecimal_ReportsSuccess(string numericText)
    {
        var parseDecimalResult = DecimalParser.ParseDecimal(numericText);

        var expectedDecimal = decimal.Parse(numericText, CultureInfo.InvariantCulture);
        parseDecimalResult.Match(
            Succ: parsedDecimal => Assert.Equal(expectedDecimal, parsedDecimal),
            Fail: error => Assert.Fail($"Unexpected failure: '{error.Message}'"));
    }
    
    [Theory]
    [InlineData("75.3O")] // The letter 'O' not the digit zero '0'
    [InlineData("-83.4f")]
    public void NonNumericText_ParseDecimal_ReportsError(string nonNumericText)
    {
        var parseDecimalResult = DecimalParser.ParseDecimal(nonNumericText);
        
        parseDecimalResult.Match(
            Succ: _ => Assert.Fail($"Expected parse failure of '{nonNumericText}'. Unexpectedly succeeded."),
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

    [Theory]
    [InlineData("26.4", "75.2", ",")]
    [InlineData("32.6", "-152.6", ", ")]
    [InlineData("\t-29.1", "61.6", ", ")]
    [InlineData("43.9", "61.6\n", ",")]
    public void ValidLatitudeAndLongitude_ParseCoordinate_ReportSuccess(string latitudeText, 
                                                                        string longitudeText, 
                                                                        string separatorText)
    {
        var latitudeAndLongitudeText = latitudeText + separatorText + longitudeText;
        var actualGeoCoordinate = GeoCoordinateParser.ParseCoordinate(latitudeAndLongitudeText);
        
        var expectedLatitudeValue = decimal.Parse(latitudeText, 
                                                  NumberStyles.Float, CultureInfo.InvariantCulture);
        var expectedLongitudeValue = decimal.Parse(longitudeText, 
                                                   NumberStyles.Float, CultureInfo.InvariantCulture);

        actualGeoCoordinate.Match(
            Succ: geoCoordinate =>
            {
                Assert.Equal(expectedLatitudeValue, geoCoordinate.Latitude.Degrees);
                Assert.Equal(expectedLongitudeValue, geoCoordinate.Longitude.Degrees);
            }, 
            Fail: error => Assert.Fail(error.Message));
    }

    [Theory]
    [InlineData("66.98e", "-76.9", ",")]
    [InlineData("76.2", "119.7s", ",")]
    public void InvalidLatitudeOrLongitude_ParseCoordinate_ReportFailure(string latitudeText, 
                                                                        string longitudeText, 
                                                                        string separatorText)
    {
        var latitudeAndLongitudeText = latitudeText + separatorText + longitudeText;
        var actualGeoCoordinate = GeoCoordinateParser.ParseCoordinate(latitudeAndLongitudeText);

        actualGeoCoordinate.Match(
            Succ: _ => Assert.Fail($"Unexpected success for '{latitudeAndLongitudeText}'"),
            Fail: error => Assert.Contains("Failed to parse decimal", error.Message));
    }

    [Fact]
    public void TooLargeCoordinateCount_ParseCoordinate_ReportFailure()
    {
        const string tooManyLatitudeAndLongitudeText = "81.3,-39.0,-12.5";
        var actualGeoCoordinate = 
            GeoCoordinateParser.ParseCoordinate(tooManyLatitudeAndLongitudeText);

        actualGeoCoordinate.Match(
            Succ: _ => Assert.Fail($"Unexpected success for '{tooManyLatitudeAndLongitudeText}'"),
            Fail: error => Assert.Contains("Expected 'lat,lng' but got", error.Message));
    }

    [Fact]
    public void TooFewCoordinateCount_ParseCoordinate_ReportFailure()
    {
        const string tooManyLatitudeAndLongitudeText = "-66.5";
        var actualGeoCoordinate = 
            GeoCoordinateParser.ParseCoordinate(tooManyLatitudeAndLongitudeText);

        actualGeoCoordinate.Match(
            Succ: _ => Assert.Fail($"Unexpected success for '{tooManyLatitudeAndLongitudeText}'"),
            Fail: error => Assert.Contains("Expected 'lat,lng' but got", error.Message));
    }

    [Fact]
    public void BothCoordinateInvalid_ParseCoordinate_ReportLatitudeFailure()
    {
        // Both capital 'O's instead of zeros
        const string bothCoordinatesInvalid = "7O.6,-145.O";
        var actualGeoCoordinate = 
            GeoCoordinateParser.ParseCoordinate(bothCoordinatesInvalid);

        actualGeoCoordinate.Match(
            Succ: _ => Assert.Fail($"Unexpected success for '{bothCoordinatesInvalid}'"),
            Fail: error => Assert.Contains("Failed to parse decimal", error.Message));
    }
}

public readonly record struct GeoCoordinate(Latitude Latitude, Longitude Longitude);

public static class GeoCoordinateParser
{
    public static Fin<GeoCoordinate> ParseCoordinate(string latitudeAndLongitudeText) =>
        latitudeAndLongitudeText.Split(',', StringSplitOptions.TrimEntries) is [var latitudeText, var longitudeText]
            ? Latitude.ParseLatitude(latitudeText)
                      .Bind(lat => Longitude.ParseLongitude(longitudeText)
                                            .Map(lng => new GeoCoordinate(lat, lng)))
            : FinFail<GeoCoordinate>(Error.New($"Expected 'lat,lng' but got: '{latitudeAndLongitudeText}'"));
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
                         out var value)
            ? FinSucc(value)
            : FinFail<decimal>(Error.New($"Failed to parse decimal: '{candidateText}'"));
}
