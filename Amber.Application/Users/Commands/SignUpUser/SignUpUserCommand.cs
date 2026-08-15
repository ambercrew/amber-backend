using Amber.Application.Users.Dto;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.SignUpUser;

public record SignUpUserCommand(SignUpDto SignUpDto) : ICommand<UserInformationDto>;
