namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public static class WordFrequency
{
    public static Map<string, int> Create() => new();
    public static Option<int> CountOf(Map<string, int> dictionary, string toFind) => dictionary.Find(toFind);

    public static Map<string, int> Count(string text) =>
        toSeq(text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Fold(Map<string, int>(), (accumulator, word) => accumulator.AddOrUpdate(word, cur => cur + 1, () => 1));
}

public class WordFrequencyTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void InitializedCounter_Count_ReturnsNone()
    {
        var dictionary = WordFrequency.Create();
        
        var counter = WordFrequency.CountOf(dictionary,"voluptatum");
        
        Assert.True(counter.IsNone);
    }

    [Fact]
    public void Count_TalliesRepeatedWord()
    {
        var frequencies = WordFrequency.Count("the cat sat the mat the");
        LangExtAssert.Equal(Some(3), WordFrequency.CountOf(frequencies, "the"));
        
    }
}
