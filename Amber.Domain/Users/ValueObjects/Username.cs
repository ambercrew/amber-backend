using System.Text.RegularExpressions;
using Amber.Domain.Common;

namespace Amber.Domain.Users.ValueObjects;

public partial class Username : ValueObject
{
    [GeneratedRegex("^[a-zA-Z0-9]([a-zA-Z0-9]|-){1,28}[a-zA-Z0-9]$")]
    public static partial Regex UsernameValidationRegex();

    private const string UsernameConstraintMessage =
        "The username must start and end with a letter or a number, and can only"
        + " contain letters, numbers and dash (-) and have a length of maximum 30 characters.";

    public string Value { get; init; } = null!;

    // Private parameterless constructor for EF Core
    private Username() { }

    public Username(string value)
    {
        value = value.Trim();

        if (!UsernameValidationRegex().IsMatch(value))
        {
            throw new InvalidOperationException(UsernameConstraintMessage);
        }

        Value = value.ToLower();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
