namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

// !!!DO NOT DO IN REAL CODE!!! (This implementation holds the password in **plain text**.)
public sealed record ValidatedPassword
{
    public string Password { get; }
    
    private ValidatedPassword(string validatedPassword) => Password = validatedPassword;
    
    public static Validation<string, ValidatedPassword> Create(string candidatePassword) =>
        Success<string, ValidatedPassword>(new ValidatedPassword(candidatePassword));
}
public class PasswordValidationTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void ValidatePassword_Valid_ReturnsSuccess()
    {
        const string candidatePassword = "abc123Def";
        var result = ValidatedPassword.Create(candidatePassword);

        result.Match(
            Succ: validatePassword => Assert.Equal(candidatePassword, validatePassword.Password),
            Fail: errors => Assert.Fail($"Expected success but got {string.Join("; ", errors)}")
            );
    }
}