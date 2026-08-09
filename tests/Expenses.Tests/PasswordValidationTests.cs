namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

// !!!DO NOT DO IN REAL CODE!!! (This implementation holds the password in **plain text**.)
public sealed record ValidatedPassword
{
    public string Password { get; }
    
    private ValidatedPassword(string validatedPassword) => Password = validatedPassword;

    private const int MinimumPasswordLength = 8;

    private static Validation<string, Unit> ValidateMinimumLength(string candidatePassword) =>
        candidatePassword.Length >= MinimumPasswordLength
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>($"Password must be at least {MinimumPasswordLength} characters long.");

    private static Validation<string, Unit> ValidateHasDigits(string candidatePassword) =>
        candidatePassword.Any(char.IsAsciiDigit)
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>("Password must contain at least 1 digit.");

    private static Validation<string, Unit> ValidateHasUpperCase(string candidatePassword) =>
        candidatePassword.Any(char.IsUpper)
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>("Password must contain at least 1 uppercase letter.");

    private static Validation<string, Unit> ValidateHasLowerCase(string candidatePassword) =>
        candidatePassword.Any(char.IsLower)
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>("Password must contain at least 1 lowercase letter.");
    
    public static Validation<string, ValidatedPassword> Create(string candidatePassword) =>
        // NOT Tuple(singleValue).Apply - silently always succeeds. See 
        // notes for phase 3.
        (ValidateMinimumLength(candidatePassword), 
            ValidateHasDigits(candidatePassword), 
            ValidateHasUpperCase(candidatePassword),
            ValidateHasLowerCase(candidatePassword))
            .Apply((_, _, _, _) => new ValidatedPassword(candidatePassword));
}
public class PasswordValidationTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void ValidatedPassword_Valid_ReturnsSuccess()
    {
        const string candidatePassword = "abc123Def";
        var result = ValidatedPassword.Create(candidatePassword);

        result.Match(
            Succ: validatedPassword => Assert.Equal(candidatePassword, validatedPassword.Password),
            Fail: errors => Assert.Fail($"Expected success but got {string.Join("; ", errors)}")
            );
    }

    [Fact]
    public void ValidatedPassword_PasswordTooShort_ReturnsFail()
    {
        const string candidatePassword = "aBc123";
        var result = ValidatedPassword.Create(candidatePassword);
        
        result.Match(
            Succ: unexpectedlyPassingPassword => 
                Assert.Fail($"Password, `{unexpectedlyPassingPassword.Password}`, unexpectedly passed"),
            Fail: errors =>
            {
                Assert.Multiple(
                    () => Assert.Equal(1, errors.Count),
                    () => Assert.Contains("at least 8 characters", errors[0]));
            });
    }

    [Fact]
    public void ValidatedPassword_PasswordNoNumbers_ReturnsFail()
    {
        const string candidatePassword = "aBcDefghI";
        var result = ValidatedPassword.Create(candidatePassword);
        
        result.Match(
            Succ: unexpectedlyPassingPassword => 
                Assert.Fail($"Password, `{unexpectedlyPassingPassword.Password}`, unexpectedly passed"),
            Fail: errors =>
            {
                Assert.Multiple(
                    () => Assert.Equal(1, errors.Count),
                    () => Assert.Contains("at least 1 digit", errors[0]));
            });
    }

    [Fact]
    public void ValidatedPassword_PasswordShortNoNumbers_ReturnsFail()
    {
        const string candidatePassword = "aBcDef";
        var result = ValidatedPassword.Create(candidatePassword);

        result.Match(
            Succ: unexpectedlyPassingPassword =>
                Assert.Fail($"Password, `{unexpectedlyPassingPassword.Password}`, unexpectedly passed"),
            Fail: errors =>
                Assert.Multiple(
                    () => Assert.Equal(2, errors.Count),
                    // The following sequence of tests relies on errors
                    // collected in the same order as the original sequence.
                    () => Assert.Contains("at least 8 characters", errors[0]),
                    () => Assert.Contains("at least 1 digit", errors[1])
                    ));
    }

    [Fact]
    public void ValidatedPassword_AllFail_ReturnsAllFailures()
    {
        const string candidatePassword = "+";
        var result = ValidatedPassword.Create(candidatePassword);

        result.Match(
            Succ: unexpectedlyPassingPassword =>
                Assert.Fail($"Password, `{unexpectedlyPassingPassword.Password}`, unexpectedly passed"),
            Fail: errors =>
                Assert.Multiple(
                    () => Assert.Equal(4, errors.Count),
                    // The following sequence of tests relies on errors
                    // collected in the same order as the original sequence.
                    () => Assert.Contains("at least 8 characters", errors[0]),
                    () => Assert.Contains("at least 1 digit", errors[1]),
                    () => Assert.Contains("at least 1 uppercase letter", errors[2]),
                    () => Assert.Contains("at least 1 lowercase letter", errors[3])
                ));
    }
}
