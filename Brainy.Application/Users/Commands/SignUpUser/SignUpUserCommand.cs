using Brainy.Application.Users.Dto;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.SignUpUser;

public record SignUpUserCommand(SignUpDto SignUpDto) : ICommand<UserInformationDto>;
