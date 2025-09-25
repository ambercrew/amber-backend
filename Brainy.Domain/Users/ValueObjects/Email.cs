using System.ComponentModel.DataAnnotations;
using Brainy.Domain.Common;

namespace Brainy.Domain.Users.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; init; } = null!;

    // Private parameterless constructor for EF Core.
    private Email() { }

    public Email(string value)
    {
        value = value.Trim();

        if (!new EmailAddressAttribute().IsValid(value))
        {
            throw new InvalidOperationException($"'{value}' is not a valid email!");
        }

        Value = value.ToLower();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
