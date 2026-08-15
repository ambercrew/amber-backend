using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.SignOutUser;

public record SignOutUserCommand(Username Username) : ICommand;
