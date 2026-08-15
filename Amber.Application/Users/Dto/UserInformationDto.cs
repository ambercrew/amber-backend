using Amber.Domain.Users.Entities;

namespace Amber.Application.Users.Dto;

public record UserInformationDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    bool IsEmailVerified
)
{
    public static UserInformationDto FromUser(User user) =>
        new(
            Id: user.Id,
            Username: user.Username.Value,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Email: user.Email.Value,
            IsEmailVerified: user.IsEmailVerified
        );
}
