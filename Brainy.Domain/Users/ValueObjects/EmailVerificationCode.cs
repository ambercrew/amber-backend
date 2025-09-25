using Brainy.Domain.Common;

namespace Brainy.Domain.Users.ValueObjects;

public class EmailVerificationCode : ValueObject
{
    public const int MaxLength = 8;

    public string Value { get; init; }

    // Private parameterless constructor for EF Core
#pragma warning disable CS8618, CS9264
    private EmailVerificationCode() { }
#pragma warning restore CS8618, CS9264

    public EmailVerificationCode(string value)
    {
        if (value.Length != MaxLength)
        {
            throw new InvalidOperationException(
                "The email verification code has incorrect length."
            );
        }

        Value = value.ToLower();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
