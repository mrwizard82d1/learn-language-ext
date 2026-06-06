namespace Expenses.Tests;

public static class LangExtAssert
{
    // Structural-equality assert that renders failures via ToString() instead
    // xUnit's IEnumerable collection formatter (the `[[...]]` "dump"). Works
    // for **any** type - `Option<t>`, `Either<L, R>`, `Fin<T>`,
    // `Validation<F, S>`, and so on. This helper works because
    // `EqualityComparer<T>.Default` uses each type's `IEquatable`.
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Assert.Fail($"Values differ.{Environment.NewLine}" +
                        $"  expected: {expected}{Environment.NewLine}" +
                        $"  actual:   {actual}");
        }
    }
}
