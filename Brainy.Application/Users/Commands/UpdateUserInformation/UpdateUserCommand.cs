using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.UpdateUserInformation;

public record UpdateUserCommand(
    UpdateUserInformationDto UpdateUserInformationDto,
    Username Username
) : ICommand;
