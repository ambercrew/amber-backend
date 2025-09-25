using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.SignOutUser;

public record SignOutUserCommand(Username Username) : ICommand;
