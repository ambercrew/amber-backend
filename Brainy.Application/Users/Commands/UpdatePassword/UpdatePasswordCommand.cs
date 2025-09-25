using Brainy.Application.Users.Dto;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.UpdatePassword;

public record UpdatePasswordCommand(UpdatePasswordDto UpdatePasswordDto, Username Username)
    : ICommand<UserInformationDto>;
