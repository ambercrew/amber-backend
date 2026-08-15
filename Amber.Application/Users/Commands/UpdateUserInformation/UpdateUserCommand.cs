using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.UpdateUserInformation;

public record UpdateUserCommand(
    UpdateUserInformationDto UpdateUserInformationDto,
    Username Username
) : ICommand;
