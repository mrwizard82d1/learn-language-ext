namespace Expenses.Tests;

using LanguageExt;

public static class WordFrequency
{
    public static Map<string, int> Create() => new();
    public static Option<int> Count(Map<string, int> dictionary, string toFind) => dictionary.Find(toFind);
}

public class WordFrequencyTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void InitializedCounter_Count_ReturnsNone()
    {
        var dictionary = WordFrequency.Create();
        
        var counter = WordFrequency.Count(dictionary,"voluptatum");
        
        Assert.True(counter.IsNone);
    }
}
