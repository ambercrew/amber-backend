using Amber.Domain.Users.Entities;
using Amber.Domain.Users.ValueObjects;

namespace Amber.TestUtils.Users;

public static class UserTestUtils
{
    public static User CreateUser(
        string username,
        Guid? id = null,
        Email? email = null,
        string? firstName = null,
        string? lastName = null,
        HashedPassword? password = null,
        bool hasPassword = true,
        DateTime? signOutDate = null,
        bool? isEmailVerified = null,
        EmailVerificationCode? emailVerificationCode = null,
        DateTime? registrationDate = null,
        string? googleId = null
    )
    {
        return new User(
            id: id ?? Guid.NewGuid(),
            username: new Username(username),
            email: email ?? new Email($"{username}@test.com"),
            firstName: firstName ?? "first-name",
            lastName: lastName ?? "last-name",
            password: hasPassword
                ? password ?? new HashedPassword(new PlainPassword("testPassword123"))
                : null,
            signOutDate: signOutDate ?? DateTime.UtcNow,
            isEmailVerified: isEmailVerified ?? false,
            emailVerificationCode: emailVerificationCode ?? new EmailVerificationCode("12345678"),
            lastDateTimeOfSentVerificationCode: DateTime.UtcNow,
            registrationDate: registrationDate ?? DateTime.UtcNow,
            googleId: googleId
        );
    }
}
