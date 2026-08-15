using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.ResendEmailVerificationCode;

public record ResendEmailVerificationCodeCommand(Username Username) : ICommand;
