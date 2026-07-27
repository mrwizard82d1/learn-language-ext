using System.Globalization;
using LanguageExt;
using LanguageExt.Common;

namespace Expenses;

public static class ExpenseParser
{
    // Correct behavior.
    public static Either<string, decimal> ParseAmountEither(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? Prelude.Right(d) 
            : Prelude.Left($"Bad amount: '{s}'");

    public static Fin<decimal> ParseAmount(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? Prelude.FinSucc(d) 
            : Prelude.FinFail<decimal>(Error.New($"Bad amount: '{s}'"));

    public static Fin<decimal> ParseAmountException(string s)
    {
        try
        {
            return Prelude.FinSucc(decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture));
        }
        catch (FormatException fe)
        {
            return Prelude.FinFail<decimal>(Error.New(fe));
        }
    }

    public static Fin<DateOnly> ParseDate(string s) =>
        DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var dt)
            ? Prelude.FinSucc(dt)
            : Prelude.FinFail<DateOnly>(Error.New($"Bad date: '{s}'"));

    public static Fin<ExpenseEntry> ParseEntry(string date, string amount, string category, string description) =>
        ParseDate(date)
            .Bind(d =>
                      ParseAmount(amount).Map(amt => new ExpenseEntry(d, amt, category, description)));
}