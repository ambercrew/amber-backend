using Amber.Domain.Users.ValueObjects;

namespace Amber.Domain.Users.Entities;

public partial class User
{
    public static readonly TimeSpan MinimumTimeBetweenResendingEmailVerificationCode =
        TimeSpan.FromMinutes(10);

    // Private parameterless constructor for EF Core
#pragma warning disable CS8618, CS9264
    private User() { }
#pragma warning restore CS8618, CS9264

    public User(
        Guid id,
        Username username,
        Email email,
        string firstName,
        string lastName,
        HashedPassword password,
        DateTime signOutDate,
        bool isEmailVerified,
        EmailVerificationCode emailVerificationCode,
        DateTime lastDateTimeOfSentVerificationCode,
        DateTime registrationDate
    )
    {
        Id = id;
        Username = username;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Password = password;
        SignOutDate = signOutDate;
        IsEmailVerified = isEmailVerified;
        EmailVerificationCode = emailVerificationCode;
        LastDateTimeOfSentVerificationCode = lastDateTimeOfSentVerificationCode;
        RegistrationDate = registrationDate;
    }

    public Guid Id { get; init; }
    public Username Username { get; init; }
    public Email Email { get; init; }
    public DateTime RegistrationDate { get; init; }

    public string FirstName
    {
        get;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException("First name name must not be empty!");
            }
            field = value;
        }
    }

    public string LastName
    {
        get;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException("Last name name must not be empty!");
            }
            field = value;
        }
    }

    public bool IsEmailVerified { get; set; }
    public EmailVerificationCode EmailVerificationCode { get; init; }

    /// <summary>
    /// Contains the date time of the last time the verification date was sent.
    /// </summary>
    public DateTime LastDateTimeOfSentVerificationCode { get; set; }

    public HashedPassword Password { get; set; }
    public DateTime SignOutDate { get; set; }

    /// <returns>
    /// True if it is okay to send the verification code, otherwise returns false.
    /// Be aware that this method may change the value of <see cref="LastDateTimeOfSentVerificationCode" />
    /// </returns>
    public bool RequestVerificationCodeResend(DateTime now)
    {
        if (
            now - LastDateTimeOfSentVerificationCode
            >= MinimumTimeBetweenResendingEmailVerificationCode
        )
        {
            LastDateTimeOfSentVerificationCode = now;
            return true;
        }
        return false;
    }
}
