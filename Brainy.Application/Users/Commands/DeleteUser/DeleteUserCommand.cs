using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : ICommand;
