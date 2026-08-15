namespace Amber.Application.Users.Commands.SignUpUser;

public record SignUpDto(
    string Username,
    string Password,
    string Email,
    string FirstName,
    string LastName
);
