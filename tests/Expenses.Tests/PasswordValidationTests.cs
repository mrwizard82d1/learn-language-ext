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
            : Fail<string, Unit>($"Password must be at least 8 characters long.");

    private static Validation<string, Unit> ValidateHasDigits(string candidatePassword) =>
        candidatePassword.Any(char.IsAsciiDigit)
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>($"Password must contain at least 1 digit.");
    
    
    public static Validation<string, ValidatedPassword> Create(string candidatePassword) =>
        // NOT Tuple(singleValue).Apply - silently always succeeds. See 
        // notes for phase 3.
        (ValidateMinimumLength(candidatePassword), ValidateHasDigits(candidatePassword))
            .Apply((_, _) => new ValidatedPassword(candidatePassword));
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
                Assert.Fail($"Password, `{unexpectedlyPassingPassword}`, unexpectedly passed"),
            Fail: errors =>
            {
                Assert.Multiple(
                    () => Assert.Equal(1, errors.Count),
                    () => Assert.Contains(errors,
                                          e => e.Contains($"at least 8 characters")));
            });
    }

    [Fact]
    public void ValidatedPassword_PasswordNoNumbers_ReturnsFail()
    {
        const string candidatePassword = "aBcDefghI";
        var result = ValidatedPassword.Create(candidatePassword);
        
        result.Match(
            Succ: unexpectedlyPassingPassword => 
                Assert.Fail($"Password, `{unexpectedlyPassingPassword}`, unexpectedly passed"),
            Fail: errors =>
            {
                Assert.Multiple(
                    () => Assert.Equal(1, errors.Count),
                    () => Assert.Contains(errors,
                                          e => e.Contains($"at least 1 digit")));
            });
    }
}
