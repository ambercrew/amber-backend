using Amber.Domain.Common;

namespace Amber.Domain.Users.ValueObjects;

/// <summary>Represents a password that is not hashed.</summary>
public class PlainPassword : ValueObject
{
    public string Value { get; init; } = null!;

    private const string PasswordConstraintsMessage =
        "The password must contain at least one small letter, one capital letter and"
        + " one number and the length must be at between 8 and 25.";

    // Private parameterless constructor for EF Core.
    private PlainPassword() { }

    public PlainPassword(string value)
    {
        if (
            8 <= value.Length
            && value.Length <= 25
            && value.Any(char.IsLower)
            && value.Any(char.IsUpper)
            && value.Any(char.IsNumber)
        )
        {
            Value = value;
        }
        else
        {
            throw new InvalidOperationException(PasswordConstraintsMessage);
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
