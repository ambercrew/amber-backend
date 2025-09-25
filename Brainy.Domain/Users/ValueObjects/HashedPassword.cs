using Brainy.Domain.Common;
using BC = BCrypt.Net.BCrypt;

namespace Brainy.Domain.Users.ValueObjects;

public class HashedPassword : ValueObject
{
    public string Value { get; init; } = null!;

    // Private parameterless constructor for EF Core.
    private HashedPassword() { }

    public HashedPassword(PlainPassword password)
    {
        Value = BC.HashPassword(password.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public bool VerifyEqual(PlainPassword plainPassword)
    {
        return BC.Verify(plainPassword.Value, Value);
    }
}
