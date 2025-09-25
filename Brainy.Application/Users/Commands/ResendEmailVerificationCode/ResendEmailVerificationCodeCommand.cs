using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.ResendEmailVerificationCode;

public record ResendEmailVerificationCodeCommand(Username Username) : ICommand;
