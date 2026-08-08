namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

// !!!DO NOT DO IN REAL CODE!!! (This implementation holds the password in **plain text**.)
public sealed record ValidatedPassword
{
    public string Password { get; }
    
    private ValidatedPassword(string validatedPassword) => Password = validatedPassword;

    private const int MinimumPasswordLength = 8;

    private static Validation<string, Unit> ValidatePasswordLength(string candidatePassword) =>
        candidatePassword.Length >= MinimumPasswordLength
            ? Success<string, Unit>(unit)
            : Fail<string, Unit>($"Password must be at least 8 characters long.");
    
    
    public static Validation<string, ValidatedPassword> Create(string candidatePassword) =>
        // Although I expected the following code to "pass" by producing a
        // failure for my "abc123" password (too short), it is actually a
        // "two wrongs make a right" **anomaly**. (See the
        // `phase-03-validation.md` `Notes`.) 
        // Tuple(ValidatePasswordLength(candidatePassword))
        //     .Apply((_) => new ValidatedPassword(candidatePassword));
        ValidatePasswordLength(candidatePassword)
            // Remember, `Map` respects "the two tracks". If
            // `ValidatePasswordLength()` returns `Success`, it invokes the
            // function passed to `Map`. If it returns `Fail`, it passes that
            // failure through **unchanged**.
            .Map(_ => new ValidatedPassword(candidatePassword));
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
}
