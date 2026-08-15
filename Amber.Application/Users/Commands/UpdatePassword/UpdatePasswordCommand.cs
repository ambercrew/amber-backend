using Amber.Application.Users.Dto;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.UpdatePassword;

public record UpdatePasswordCommand(UpdatePasswordDto UpdatePasswordDto, Username Username)
    : ICommand<UserInformationDto>;
