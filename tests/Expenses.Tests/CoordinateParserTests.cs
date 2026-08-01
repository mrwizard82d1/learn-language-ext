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
        var actualLatitude = CoordinateParser.ParseLat(latitudeText);
       
        LangExtAssert.Equal(
            FinSucc(decimal.Parse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture)), 
            actualLatitude);
    }

    [Fact]
    public void NonNumericLatitudeText_ParseLatitude_ReportsError()
    {
        // The letter 'O' not the digit zero '0'
        const string nonNumericLatitudeText = "75.3O";
        var nonNumericLatitude = CoordinateParser.ParseLat(nonNumericLatitudeText);
        
        nonNumericLatitude.Match(
            Succ: _ => 
                throw new InvalidOperationException($"Expected parse failure of '{nonNumericLatitudeText}'. " 
                                                    + "Unexpectedly succeeded."),
            Fail: errorText => Assert.Equal($"Failed to parse latitude: '{nonNumericLatitudeText}'", errorText));
    }

    [Fact]
    public void LatitudeOutOfRange_ParseLatitude_ReportsError()
    {
        const string outOfRangeLatitudeText = "90.00001";
        var outOfRangeLatitudeResult = CoordinateParser.GetLatitude(outOfRangeLatitudeText);
        
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
    public static Fin<Latitude> GetLatitude(string latitudeText) =>
        ParseLat(latitudeText).Bind(Latitude.Create);
    
    public static Fin<decimal> ParseLat(string latitudeText) =>
        decimal.TryParse(latitudeText,
                         NumberStyles.Float,
                         CultureInfo.InvariantCulture,
                         out var candidateLatitude)
            ? FinSucc(candidateLatitude)
            : FinFail<decimal>(Error.New($"Failed to parse latitude: '{latitudeText}'"));
}

public readonly record struct Latitude
{
    public decimal Degrees { get; }

    private Latitude(decimal degrees) => Degrees = degrees;

    public static Fin<Latitude> Create(decimal degrees) =>
        degrees is >= -90.0m and <= 90.0m
            ? FinSucc(new Latitude(degrees))
            : FinFail<Latitude>(Error.New($"Parsed latitude, '{degrees}', is out of range"));
}
